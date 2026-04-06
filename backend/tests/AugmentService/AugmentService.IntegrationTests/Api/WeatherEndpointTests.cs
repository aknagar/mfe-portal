using System.Net;
using System.Net.Http.Headers;
using AugmentService.Infrastructure.Data;
using AugmentService.Infrastructure.ProductData;
using AugmentService.Infrastructure.WeatherData;
using Common.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AugmentService.IntegrationTests.Api;

/// <summary>
/// Custom WebApplicationFactory with persistent SQLite connections and no Dapr runtime.
///
/// Authentication mirrors production:
///   - The app runs with Microsoft.Identity.Web (same as production).
///   - ConfigureWebHost swaps the Entra ID JwtBearer validator for a local HS256 validator
///     via <see cref="TestAuthServiceExtensions.ReplaceWithTestJwtHandler"/>.
///   - Tests must attach a Bearer token from <see cref="TestTokenFactory"/>; requests
///     without a token receive 401, just as they would in production.
/// </summary>
public class WeatherTestFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _productConn;
    private readonly SqliteConnection _weatherConn;
    private readonly SqliteConnection _userConn;

    public WeatherTestFactory()
    {
        _productConn = new SqliteConnection("Data Source=:memory:");
        _weatherConn = new SqliteConnection("Data Source=:memory:");
        _userConn    = new SqliteConnection("Data Source=:memory:");

        _productConn.Open();
        _weatherConn.Open();
        _userConn.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Supply AzureAd values so AddMicrosoftIdentityWebApiAuthentication doesn't
            // throw on startup; the actual Entra ID validator is replaced below.
            var overrides = new Dictionary<string, string?>(TestAuthConstants.ConfigOverrides)
            {
                ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableHealthChecks"] = "true",
                ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableTracing"]      = "true",
                ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableMetrics"]      = "true",
            };
            config.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
            IntegrationTestHelpers.RemoveDbContextDescriptors(services,
                typeof(ProductDataContext),
                typeof(WeatherDatabaseContext),
                typeof(UserDbContext));

            services.Configure<AugmentService.Infrastructure.InfrastructureConfig>(cfg =>
            {
                cfg.ConnectionString           = "Data Source=:memory:";
                cfg.EnableSensitiveDataLogging = false;
            });

            services.AddDbContext<ProductDataContext>(options  => options.UseSqlite(_productConn));
            services.AddDbContext<WeatherDatabaseContext>(options => options.UseSqlite(_weatherConn));
            services.AddDbContext<UserDbContext>(options       => options.UseSqlite(_userConn));

            IntegrationTestHelpers.ReplaceDaprServices(services);

            // Replace the Entra ID JwtBearer validator with a local HS256 validator.
            // All other auth/authz middleware (token parsing, claims mapping, [Authorize]
            // enforcement) runs unchanged — same as production.
            services.ReplaceWithTestJwtHandler();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var svc = scope.ServiceProvider;
            IntegrationTestHelpers.InitDb<ProductDataContext>(svc);
            IntegrationTestHelpers.InitDb<WeatherDatabaseContext>(svc);
            IntegrationTestHelpers.InitDb<UserDbContext>(svc);

            // Seed a known forecast so GET /weather/{date} returns 200.
            using var seedScope = sp.CreateScope();
            var weatherDb = seedScope.ServiceProvider.GetRequiredService<WeatherDatabaseContext>();
            var forecast = AugmentService.Core.Forecast.New(new DateOnly(2024, 1, 1), 20, "Sunny").Value;
            weatherDb.Forecasts.Add(forecast);
            weatherDb.SaveChanges();
        });
    }

    /// <summary>Creates an HttpClient pre-configured with a valid test Bearer token.</summary>
    public HttpClient CreateAuthenticatedClient(
        string?   userId  = null,
        string?   email   = null,
        string?   name    = null,
        string[]? roles   = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            TestTokenFactory.CreateBearerHeader(userId, email, name, roles);
        return client;
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

public class WeatherEndpointTests : IClassFixture<WeatherTestFactory>
{
    private readonly WeatherTestFactory _factory;

    public WeatherEndpointTests(WeatherTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetWeatherForecast_WithValidToken_ReturnsOk()
    {
        // Arrange — authenticated client with a valid test token
        using var client = _factory.CreateAuthenticatedClient();

        // Act — a forecast for 2024-01-01 is seeded in the factory
        var response = await client.GetAsync("/weather/2024-01-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetWeatherForecast_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange — unauthenticated client (no Authorization header)
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/weather/2024-01-01");

        // Assert — 401, same behaviour as production
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetWeatherForecast_WithExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateExpiredToken());

        // Act
        var response = await client.GetAsync("/weather/2024-01-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetHealth_ReturnsHealthy()
    {
        // Health endpoint is [AllowAnonymous] — no token required
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
