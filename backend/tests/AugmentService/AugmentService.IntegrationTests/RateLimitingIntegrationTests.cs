using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AugmentService.Infrastructure.ProductData;
using AugmentService.Infrastructure.WeatherData;
using AugmentService.Infrastructure.Data;
using AugmentService.Api.Workflows;
using Dapr.Client;
using Dapr.Workflow;
using Moq;
using Xunit;

namespace AugmentService.IntegrationTests;

/// <summary>
/// Rate-limiting integration tests.
/// Each test creates its own factory instance so that the rate-limit window
/// is always fresh at the start of every test (no shared server state).
/// </summary>
public class RateLimitingIntegrationTests
{
    /// <summary>
    /// Creates a fresh <see cref="RateLimitingWebFactory"/> for a single test.
    /// Disposing it tears down the server and releases the SQLite connections.
    /// </summary>
    private static RateLimitingWebFactory CreateFactory() => new();

    [Fact]
    public async Task RateLimiter_EnforcesLimit_Returns429AfterLimitExceeded()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        const int permitLimit = 5;

        // Act - Make requests up to the limit
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < permitLimit + 2; i++)
        {
            responses.Add(await client.GetAsync("/api/Product"));
        }

        // Assert
        // First 5 requests should succeed (not 429)
        for (int i = 0; i < permitLimit; i++)
        {
            Assert.NotEqual(HttpStatusCode.TooManyRequests, responses[i].StatusCode);
        }

        // Subsequent requests should be rate limited
        Assert.Equal(HttpStatusCode.TooManyRequests, responses[permitLimit].StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, responses[permitLimit + 1].StatusCode);

        // Check for Retry-After header
        Assert.True(responses[permitLimit].Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task RateLimiter_Returns429WithCorrectJsonResponse()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        const int permitLimit = 5;

        // Act - Exhaust the permit limit then make one more request
        for (int i = 0; i < permitLimit; i++)
        {
            await client.GetAsync("/api/Product");
        }

        var response = await client.GetAsync("/api/Product");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("TooManyRequests", json.GetProperty("error").GetString());
        Assert.Equal(429, json.GetProperty("statusCode").GetInt32());
        Assert.True(json.TryGetProperty("retryAfterSeconds", out _));
        Assert.Contains("Rate limit exceeded", json.GetProperty("message").GetString());
    }

    [Fact]
    public async Task RateLimiter_DoesNotApplyToHealthChecks()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        const int attemptCount = 20; // Well over the limit

        // Act - Make many requests to health check
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < attemptCount; i++)
        {
            responses.Add(await client.GetAsync("/health"));
        }

        // Assert - All should succeed (not rate limited)
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }

    [Fact]
    public async Task RateLimiter_DoesNotApplyToAliveEndpoint()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        const int attemptCount = 20; // Well over the limit

        // Act - Make many requests to alive endpoint
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < attemptCount; i++)
        {
            responses.Add(await client.GetAsync("/alive"));
        }

        // Assert - All should succeed (not rate limited)
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }

    [Fact]
    public async Task RateLimiter_IncludesRetryAfterHeader()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        const int permitLimit = 5;

        // Act - Exhaust the limit then check the rejection response
        for (int i = 0; i < permitLimit; i++)
        {
            await client.GetAsync("/api/Product");
        }

        var response = await client.GetAsync("/api/Product");

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.True(response.Headers.Contains("Retry-After"));

        var retryAfter = response.Headers.GetValues("Retry-After").FirstOrDefault();
        Assert.NotNull(retryAfter);
        Assert.True(int.TryParse(retryAfter, out var seconds));
        Assert.True(seconds > 0 && seconds <= 60);
    }

    [Fact]
    public async Task RateLimiter_AppliesAcrossMultipleEndpoints()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        // Act - Hit different endpoints (global limiter applies to all)
        var responses = new List<HttpResponseMessage>
        {
            await client.GetAsync("/api/Product"),
            await client.GetAsync("/api/Product/1"),
            await client.GetAsync("/weather/2024-01-01"),
            await client.GetAsync("/api/Product"),
            await client.GetAsync("/api/TodoItems"),
            // This should exceed the limit (6th request)
            await client.GetAsync("/api/Product")
        };

        // Assert
        var lastResponse = responses.Last();
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
    }
}

/// <summary>
/// Custom WebApplicationFactory that keeps SQLite in-memory connections alive
/// for the duration of the test class. Each DB gets its own SqliteConnection
/// that is opened before EnsureCreated() and only disposed at teardown.
/// </summary>
public class RateLimitingWebFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _productConn;
    private readonly SqliteConnection _weatherConn;
    private readonly SqliteConnection _userConn;

    public RateLimitingWebFactory()
    {
        _productConn = new SqliteConnection("Data Source=:memory:");
        _weatherConn = new SqliteConnection("Data Source=:memory:");
        _userConn = new SqliteConnection("Data Source=:memory:");

        _productConn.Open();
        _weatherConn.Open();
        _userConn.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Enabled"] = "true",
                ["RateLimiting:PermitLimit"] = "5",
                ["RateLimiting:WindowSeconds"] = "60",
                ["RateLimiting:QueueLimit"] = "0",
                ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableHealthChecks"] = "true",
                ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableTracing"] = "true",
                ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableMetrics"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace DbContext registrations with SQLite backed by persistent connections
            IntegrationTestHelpers.RemoveDbContextDescriptors(services,
                typeof(ProductDataContext),
                typeof(WeatherDatabaseContext),
                typeof(UserDbContext));

            services.Configure<AugmentService.Infrastructure.InfrastructureConfig>(cfg =>
            {
                cfg.ConnectionString = "Data Source=:memory:";
                cfg.EnableSensitiveDataLogging = false;
            });

            // Use the same open connection for each DbContext so the schema persists
            services.AddDbContext<ProductDataContext>(options =>
                options.UseSqlite(_productConn));
            services.AddDbContext<WeatherDatabaseContext>(options =>
                options.UseSqlite(_weatherConn));
            services.AddDbContext<UserDbContext>(options =>
                options.UseSqlite(_userConn));

            // Replace Dapr with no-op mocks
            IntegrationTestHelpers.ReplaceDaprServices(services);

            // Create schemas
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var svc = scope.ServiceProvider;
            IntegrationTestHelpers.InitDb<ProductDataContext>(svc);
            IntegrationTestHelpers.InitDb<WeatherDatabaseContext>(svc);
            IntegrationTestHelpers.InitDb<UserDbContext>(svc);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _productConn.Dispose();
            _weatherConn.Dispose();
            _userConn.Dispose();
        }
        base.Dispose(disposing);
    }
}
