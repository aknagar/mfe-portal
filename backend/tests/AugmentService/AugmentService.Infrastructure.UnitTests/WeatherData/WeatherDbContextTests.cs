using AugmentService.Core;
using AugmentService.Core.Entities;
using AugmentService.Infrastructure.WeatherData;
using FluentAssertions;
using FluentResults;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AugmentService.Infrastructure.UnitTests.WeatherData;

public class WeatherDbInitializerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WeatherDatabaseContext _context;

    public WeatherDbInitializerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<WeatherDatabaseContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new WeatherDatabaseContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public void Initialize_Should_NotSeedForecasts_When_TableIsEmpty()
    {
        // The WeatherDbInitializer has an empty seed list.
        // Act
        DbInitializer.Initialize(_context);

        // Assert - no rows seeded (the list is empty)
        var count = _context.Forecasts.Count();
        count.Should().Be(0);
    }

    [Fact]
    public void Initialize_Should_NotThrow_When_TableAlreadyHasData()
    {
        // Arrange - add a forecast so the table is non-empty
        var forecastResult = Forecast.New(new DateOnly(2025, 1, 1), 20, "Sunny");
        forecastResult.IsSuccess.Should().BeTrue();
        _context.Forecasts.Add(forecastResult.Value);
        _context.SaveChanges();

        // Act - Initialize should return early without throwing
        var act = () => DbInitializer.Initialize(_context);

        // Assert
        act.Should().NotThrow();
        _context.Forecasts.Count().Should().Be(1);
    }
}

public class WeatherExtensionsTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public WeatherExtensionsTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public void CreateWeatherDbIfNotExists_Should_CreateSchemaAndRunInitializer()
    {
        // Arrange - build a minimal host that registers WeatherDatabaseContext with SQLite :memory:
        var options = new DbContextOptionsBuilder<WeatherDatabaseContext>()
            .UseSqlite(_connection)
            .Options;

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddDbContext<WeatherDatabaseContext>(opt =>
            opt.UseSqlite(_connection));

        var app = builder.Build();

        // Act
        app.CreateWeatherDbIfNotExists();

        // Assert - no exception; schema was created and initializer ran (no seed data)
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WeatherDatabaseContext>();
        var count = context.Forecasts.Count();
        count.Should().Be(0); // WeatherDbInitializer seeds an empty list
    }
}
