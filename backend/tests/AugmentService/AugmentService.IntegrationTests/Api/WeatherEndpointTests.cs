using System.Net;
using AugmentService.Infrastructure.Data;
using AugmentService.Infrastructure.ProductData;
using AugmentService.Infrastructure.WeatherData;
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
                ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableHealthChecks"] = "true",
                ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableTracing"] = "true",
                ["Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableMetrics"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            IntegrationTestHelpers.RemoveDbContextDescriptors(services,
                typeof(ProductDataContext),
                typeof(WeatherDatabaseContext),
                typeof(UserDbContext));

            services.Configure<AugmentService.Infrastructure.InfrastructureConfig>(cfg =>
            {
                cfg.ConnectionString = "Data Source=:memory:";
                cfg.EnableSensitiveDataLogging = false;
            });

            services.AddDbContext<ProductDataContext>(options =>
                options.UseSqlite(_productConn));
            services.AddDbContext<WeatherDatabaseContext>(options =>
                options.UseSqlite(_weatherConn));
            services.AddDbContext<UserDbContext>(options =>
                options.UseSqlite(_userConn));

            IntegrationTestHelpers.ReplaceDaprServices(services);

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
    private readonly HttpClient _client;

    public WeatherEndpointTests(WeatherTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetWeatherForecast_ReturnsOk()
    {
        // Act — route is /weather/{date:yyyy-MM-dd}; a forecast for 2024-01-01 is seeded in the factory.
        var response = await _client.GetAsync("/weather/2024-01-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetHealth_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
