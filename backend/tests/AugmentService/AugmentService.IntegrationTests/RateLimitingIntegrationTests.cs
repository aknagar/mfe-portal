using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AugmentService.Infrastructure.ProductData;
using AugmentService.Infrastructure.WeatherData;
using AugmentService.Infrastructure.Data;
using Xunit;

namespace AugmentService.IntegrationTests;

public class RateLimitingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RateLimitingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
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
                    // Use in-memory SQLite for testing
                    ["ConnectionStrings:productdb"] = "Data Source=InMemoryProductDb;Mode=Memory;Cache=Shared",
                    ["ConnectionStrings:weatherdb"] = "Data Source=InMemoryWeatherDb;Mode=Memory;Cache=Shared",
                    ["TestContainers:Enabled"] = "false",
                    // Disable Aspire connection string validation
                    ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableHealthChecks"] = "true",
                    ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableTracing"] = "true",
                    ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableMetrics"] = "true"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove all existing DbContext-related registrations including pools
                var descriptorsToRemove = services.Where(d =>
                    d.ServiceType.IsGenericType && (
                        d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) ||
                        d.ServiceType.Name.Contains("IDbContextPool") ||
                        d.ServiceType.Name.Contains("IScopedDbContextLease")
                    ) && (
                        d.ServiceType.GetGenericArguments().Any(t => 
                            t == typeof(ProductDataContext) ||
                            t == typeof(WeatherDatabaseContext) ||
                            t == typeof(UserDbContext))
                    )).ToList();

                // Also remove the DbContext registrations themselves
                descriptorsToRemove.AddRange(services.Where(d =>
                    d.ServiceType == typeof(ProductDataContext) ||
                    d.ServiceType == typeof(WeatherDatabaseContext) ||
                    d.ServiceType == typeof(UserDbContext)).ToList());

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Register InfrastructureConfig for contexts that need it
                services.Configure<AugmentService.Infrastructure.InfrastructureConfig>(config =>
                {
                    config.ConnectionString = "Data Source=InMemoryTestDb;Mode=Memory;Cache=Shared";
                    config.EnableSensitiveDataLogging = false;
                });

                // Add in-memory database contexts using SQLite with pooling (matching Aspire's behavior)
                services.AddDbContextPool<ProductDataContext>(options =>
                    options.UseSqlite("Data Source=InMemoryProductDb;Mode=Memory;Cache=Shared"));
                
                services.AddDbContextPool<WeatherDatabaseContext>(options =>
                    options.UseSqlite("Data Source=InMemoryWeatherDb;Mode=Memory;Cache=Shared"));
                
                services.AddDbContextPool<UserDbContext>(options =>
                    options.UseSqlite("Data Source=InMemoryUserDb;Mode=Memory;Cache=Shared"));

                // Initialize databases with schema
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                
                try
                {
                    var productDb = scopedServices.GetRequiredService<ProductDataContext>();
                    productDb.Database.OpenConnection();
                    productDb.Database.EnsureCreated();
                    
                    var weatherDb = scopedServices.GetRequiredService<WeatherDatabaseContext>();
                    weatherDb.Database.OpenConnection();
                    weatherDb.Database.EnsureCreated();
                    
                    var userDb = scopedServices.GetRequiredService<UserDbContext>();
                    userDb.Database.OpenConnection();
                    userDb.Database.EnsureCreated();
                }
                catch
                {
                    // Ignore initialization errors - tests may not need all databases
                }
            });
        });
    }

    [Fact]
    public async Task RateLimiter_EnforcesLimit_Returns429AfterLimitExceeded()
    {
        // Arrange
        var client = _factory.CreateClient();
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
        var client = _factory.CreateClient();
        const int permitLimit = 5;

        // Act - Exceed the limit
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
        var client = _factory.CreateClient();
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
        var client = _factory.CreateClient();
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
        var client = _factory.CreateClient();
        const int permitLimit = 5;

        // Act - Exceed the limit
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
        var client = _factory.CreateClient();
        const int permitLimit = 5;

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
