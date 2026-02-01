using AugmentService.Api.Routes.Weather.Endpoints;
using Application.Weather.DeleteForecast;
using FluentResults;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using Xunit;

namespace AugmentService.Api.UnitTests.Routes.Weather;

public class DeleteWeatherTests
{
    private readonly IMediator _mediator;

    public DeleteWeatherTests()
    {
        _mediator = Substitute.For<IMediator>();
    }

    [Fact]
    public async Task Should_ReturnNoContent_When_DeleteSuccessful()
    {
        // Arrange
        var date = "2025-12-25";
        _mediator.Send(Arg.Any<DeleteForecastCommand>())
            .Returns(Result.Ok());

        // Act
        var result = await DeleteWeather.Handle(_mediator, date);

        // Assert
        result.Result.Should().BeOfType<NoContent>();
    }

    [Fact]
    public async Task Should_ReturnProblem_When_DeleteFails()
    {
        // Arrange
        var date = "2025-12-25";
        _mediator.Send(Arg.Any<DeleteForecastCommand>())
            .Returns(Result.Fail("Forecast not found"));

        // Act
        var result = await DeleteWeather.Handle(_mediator, date);

        // Assert
        result.Result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result.Result;
        problemResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Should_ParseDateCorrectly_When_ValidDateProvided()
    {
        // Arrange
        var date = "2026-01-15";
        var expectedDate = new DateOnly(2026, 1, 15);
        _mediator.Send(Arg.Any<DeleteForecastCommand>())
            .Returns(Result.Ok());

        // Act
        await DeleteWeather.Handle(_mediator, date);

        // Assert
        await _mediator.Received(1).Send(Arg.Is<DeleteForecastCommand>(
            cmd => cmd.Date == expectedDate
        ));
    }

    [Fact]
    public async Task Should_CallMediatorOnce_When_HandleCalled()
    {
        // Arrange
        var date = "2025-12-25";
        _mediator.Send(Arg.Any<DeleteForecastCommand>())
            .Returns(Result.Ok());

        // Act
        await DeleteWeather.Handle(_mediator, date);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<DeleteForecastCommand>());
    }

    [Fact]
    public async Task Should_ReturnProblem_When_MultipleErrorsOccur()
    {
        // Arrange
        var date = "2025-12-25";
        _mediator.Send(Arg.Any<DeleteForecastCommand>())
            .Returns(Result.Fail(new Error("Error 1")).WithError("Error 2"));

        // Act
        var result = await DeleteWeather.Handle(_mediator, date);

        // Assert
        result.Result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result.Result;
        problemResult.ProblemDetails.Detail.Should().Contain("Error 1");
        problemResult.ProblemDetails.Detail.Should().Contain("Error 2");
    }

    [Fact]
    public async Task Should_HandleLeapYearDate_When_DateIsFebruary29()
    {
        // Arrange
        var date = "2024-02-29"; // Leap year
        var expectedDate = new DateOnly(2024, 2, 29);
        _mediator.Send(Arg.Any<DeleteForecastCommand>())
            .Returns(Result.Ok());

        // Act
        await DeleteWeather.Handle(_mediator, date);

        // Assert
        await _mediator.Received(1).Send(Arg.Is<DeleteForecastCommand>(
            cmd => cmd.Date == expectedDate
        ));
    }

    [Fact]
    public async Task Should_HandleEndOfYearDate_When_DateIsDecember31()
    {
        // Arrange
        var date = "2025-12-31";
        var expectedDate = new DateOnly(2025, 12, 31);
        _mediator.Send(Arg.Any<DeleteForecastCommand>())
            .Returns(Result.Ok());

        // Act
        await DeleteWeather.Handle(_mediator, date);

        // Assert
        await _mediator.Received(1).Send(Arg.Is<DeleteForecastCommand>(
            cmd => cmd.Date == expectedDate
        ));
    }

    [Fact]
    public async Task Should_HandleStartOfYearDate_When_DateIsJanuary1()
    {
        // Arrange
        var date = "2025-01-01";
        var expectedDate = new DateOnly(2025, 1, 1);
        _mediator.Send(Arg.Any<DeleteForecastCommand>())
            .Returns(Result.Ok());

        // Act
        await DeleteWeather.Handle(_mediator, date);

        // Assert
        await _mediator.Received(1).Send(Arg.Is<DeleteForecastCommand>(
            cmd => cmd.Date == expectedDate
        ));
    }
}
