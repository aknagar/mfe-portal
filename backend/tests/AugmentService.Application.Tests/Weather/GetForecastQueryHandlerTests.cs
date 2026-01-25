using Application.Weather.GetForecast;
using AugmentService.Core;
using AugmentService.Core.Interfaces;
using FluentAssertions;
using FluentResults;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AugmentService.Application.Tests.Weather;

public class GetForecastQueryHandlerTests
{
    private readonly IWeatherRepository _weatherRepository;
    private readonly GetForecastQueryHandler _sut;

    public GetForecastQueryHandlerTests()
    {
        _weatherRepository = Substitute.For<IWeatherRepository>();
        _sut = new GetForecastQueryHandler(_weatherRepository);
    }

    [Fact]
    public async Task Handle_WhenForecastExists_ReturnsSuccessWithForecast()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var query = new GetForecastQuery { From = date };
        var forecast = Forecast.New(date, 25, "Sunny").Value;

        _weatherRepository.GetForecastAsync(date)
            .Returns(Task.FromResult<Forecast?>(forecast));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Date.Should().Be(date);
        result.Value.TemperatureC.Should().Be(25);
        result.Value.Summary.Should().Be("Sunny");
        await _weatherRepository.Received(1).GetForecastAsync(date);
    }

    [Fact]
    public async Task Handle_WhenForecastDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var query = new GetForecastQuery { From = date };

        _weatherRepository.GetForecastAsync(date)
            .Returns(Task.FromResult<Forecast?>(null));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("No Forecast exists");
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsException_ReturnsFailure()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var query = new GetForecastQuery { From = date };

        _weatherRepository.GetForecastAsync(date)
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("Failed to get forecast");
    }
}
