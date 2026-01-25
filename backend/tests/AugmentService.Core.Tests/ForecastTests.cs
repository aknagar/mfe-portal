using AugmentService.Core;
using FluentAssertions;
using Xunit;

namespace AugmentService.Core.Tests;

public class ForecastTests
{
    [Fact]
    public void New_WithValidTemperature_ReturnsSuccessResult()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var temperatureC = 25;
        var summary = "Warm";

        // Act
        var result = Forecast.New(date, temperatureC, summary);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Date.Should().Be(date);
        result.Value.TemperatureC.Should().Be(temperatureC);
        result.Value.Summary.Should().Be(summary);
        result.Value.Id.Should().NotBe(Guid.Empty);
        result.Value.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(0)]
    [InlineData(60)]
    public void New_WithBoundaryTemperature_ReturnsSuccessResult(int temperature)
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var summary = "Test";

        // Act
        var result = Forecast.New(date, temperature, summary);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TemperatureC.Should().Be(temperature);
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(-100)]
    [InlineData(61)]
    [InlineData(100)]
    public void New_WithTemperatureBelowMinus90OrAbove60_ReturnsFailure(int temperature)
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var summary = "Test";

        // Act
        var result = Forecast.New(date, temperature, summary);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("Temperature must be between -90 and 60");
    }

    [Fact]
    public void New_WithNullSummary_ReturnsSuccessResult()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var temperatureC = 20;
        string? summary = null;

        // Act
        var result = Forecast.New(date, temperatureC, summary);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.Should().BeNull();
    }

    [Fact]
    public void Delete_SetsIsDeletedToTrue()
    {
        // Arrange
        var forecast = Forecast.New(DateOnly.FromDateTime(DateTime.Today), 20, "Sunny").Value;
        forecast.IsDeleted.Should().BeFalse();

        // Act
        forecast.Delete();

        // Assert
        forecast.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void New_GeneratesUniqueIds()
    {
        // Arrange & Act
        var forecast1 = Forecast.New(DateOnly.FromDateTime(DateTime.Today), 20, "Sunny").Value;
        var forecast2 = Forecast.New(DateOnly.FromDateTime(DateTime.Today), 20, "Sunny").Value;

        // Assert
        forecast1.Id.Should().NotBe(forecast2.Id);
    }
}
