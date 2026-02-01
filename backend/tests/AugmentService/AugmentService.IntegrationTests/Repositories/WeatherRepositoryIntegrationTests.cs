using AugmentService.Core;
using AugmentService.Infrastructure.Repositories;
using AugmentService.Infrastructure.WeatherData;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace AugmentService.IntegrationTests.Repositories;

/// <summary>
/// Integration tests for WeatherRepository using TestContainers PostgreSQL.
/// Tests actual database interactions including soft deletes and data persistence.
/// </summary>
public class WeatherRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private WeatherDatabaseContext _context = null!;
    private WeatherRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<WeatherDatabaseContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _context = new WeatherDatabaseContext(options);
        await _context.Database.EnsureCreatedAsync();

        _repository = new WeatherRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_AddAndRetrieveForecast_When_ValidForecastProvided()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var forecastResult = Forecast.New(date, 25, "Sunny");
        var forecast = forecastResult.Value;

        // Act
        await _repository.AddForecastAsync(forecast);
        await _context.SaveChangesAsync();

        var retrieved = await _repository.GetForecastAsync(date);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Date.Should().Be(date);
        retrieved.TemperatureC.Should().Be(25);
        retrieved.Summary.Should().Be("Sunny");
        retrieved.IsDeleted.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_NotReturnDeletedForecast_When_ForecastIsSoftDeleted()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var forecastResult = Forecast.New(date, 20, "Cloudy");
        var forecast = forecastResult.Value;

        await _repository.AddForecastAsync(forecast);
        await _context.SaveChangesAsync();

        // Act - Soft delete the forecast
        forecast.Delete();
        await _context.SaveChangesAsync();

        var retrieved = await _repository.GetForecastAsync(date);

        // Assert
        retrieved.Should().BeNull("soft deleted forecasts should not be returned");

        // Verify it's still in database but marked as deleted
        var stillInDb = await _context.Forecasts.FindAsync(forecast.Id);
        stillInDb.Should().NotBeNull();
        stillInDb!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_PersistMultipleForecasts_When_AddedSequentially()
    {
        // Arrange
        var baseDate = DateOnly.FromDateTime(DateTime.Today);
        var forecast1 = Forecast.New(baseDate, 20, "Day 1").Value;
        var forecast2 = Forecast.New(baseDate.AddDays(1), 22, "Day 2").Value;
        var forecast3 = Forecast.New(baseDate.AddDays(2), 24, "Day 3").Value;

        // Act
        await _repository.AddForecastAsync(forecast1);
        await _repository.AddForecastAsync(forecast2);
        await _repository.AddForecastAsync(forecast3);
        await _context.SaveChangesAsync();

        // Assert
        var allForecasts = await _context.Forecasts.Where(f => !f.IsDeleted).ToListAsync();
        allForecasts.Should().HaveCount(3);
        allForecasts.Select(f => f.Summary).Should().Contain(new[] { "Day 1", "Day 2", "Day 3" });
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_HandleExtremeTemperatures_When_WithinValidRange()
    {
        // Arrange
        var hotDate = DateOnly.FromDateTime(DateTime.Today);
        var coldDate = hotDate.AddDays(1);

        var hotForecast = Forecast.New(hotDate, 60, "Max Temp").Value; // Max valid: 60°C
        var coldForecast = Forecast.New(coldDate, -90, "Min Temp").Value; // Min valid: -90°C

        // Act
        await _repository.AddForecastAsync(hotForecast);
        await _repository.AddForecastAsync(coldForecast);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedHot = await _context.Forecasts.FindAsync(hotForecast.Id);
        retrievedHot.Should().NotBeNull();
        retrievedHot!.TemperatureC.Should().Be(60);

        var retrievedCold = await _context.Forecasts.FindAsync(coldForecast.Id);
        retrievedCold.Should().NotBeNull();
        retrievedCold!.TemperatureC.Should().Be(-90);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_FilterByIsDeleted_When_MixOfActiveAndDeletedForecasts()
    {
        // Arrange
        var baseDate = DateOnly.FromDateTime(DateTime.Today);
        var activeForecast = Forecast.New(baseDate, 25, "Active").Value;
        var deletedForecast = Forecast.New(baseDate.AddDays(1), 26, "Deleted").Value;

        await _repository.AddForecastAsync(activeForecast);
        await _repository.AddForecastAsync(deletedForecast);
        await _context.SaveChangesAsync();

        deletedForecast.Delete();
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetForecastAsync(baseDate);

        // Assert
        result.Should().NotBeNull();
        result!.IsDeleted.Should().BeFalse();
        result.Summary.Should().Be("Active");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_ReturnNull_When_NoActiveForecasts()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var forecast = Forecast.New(date, 22, "To Delete").Value;

        await _repository.AddForecastAsync(forecast);
        await _context.SaveChangesAsync();

        forecast.Delete();
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetForecastAsync(date);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_PersistNullSummary_When_SummaryNotProvided()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var forecast = Forecast.New(date, 20, null).Value;

        // Act
        await _repository.AddForecastAsync(forecast);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.Forecasts.FindAsync(forecast.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Summary.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_MaintainDataIntegrity_When_ConcurrentOperations()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var forecast1 = Forecast.New(date, 20, "Concurrent 1").Value;
        var forecast2 = Forecast.New(date.AddDays(1), 22, "Concurrent 2").Value;

        // Create second context for concurrent operations
        var options = new DbContextOptionsBuilder<WeatherDatabaseContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        using var context2 = new WeatherDatabaseContext(options);
        var repository2 = new WeatherRepository(context2);

        // Act - Add from both contexts concurrently
        await _repository.AddForecastAsync(forecast1);
        await repository2.AddForecastAsync(forecast2);

        await _context.SaveChangesAsync();
        await context2.SaveChangesAsync();

        // Assert
        var allForecasts = await _context.Forecasts.ToListAsync();
        allForecasts.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_GenerateUniqueIds_When_CreatingMultipleForecasts()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var forecast1 = Forecast.New(date, 20, "Forecast 1").Value;
        var forecast2 = Forecast.New(date.AddDays(1), 22, "Forecast 2").Value;

        // Act
        await _repository.AddForecastAsync(forecast1);
        await _repository.AddForecastAsync(forecast2);
        await _context.SaveChangesAsync();

        // Assert
        forecast1.Id.Should().NotBe(forecast2.Id);
        forecast1.Id.Should().NotBeEmpty();
        forecast2.Id.Should().NotBeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_PreserveDateOnly_When_StoringAndRetrieving()
    {
        // Arrange
        var specificDate = new DateOnly(2025, 12, 25);
        var forecast = Forecast.New(specificDate, 5, "Christmas").Value;

        // Act
        await _repository.AddForecastAsync(forecast);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Forecasts.FindAsync(forecast.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Date.Should().Be(specificDate);
        retrieved.Date.Year.Should().Be(2025);
        retrieved.Date.Month.Should().Be(12);
        retrieved.Date.Day.Should().Be(25);
    }
}
