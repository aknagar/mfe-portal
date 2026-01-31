using AugmentService.Api.Routes.Weather.Endpoints;
using AugmentService.Api.Routes.Weather.Models;
using Application.Weather.GetForecast;
using FluentResults;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using Xunit;

namespace AugmentService.Api.UnitTests.Routes.Weather;

public class GetWeatherTests
{
    private readonly IMediator _mediator;

    public GetWeatherTests()
    {
        _mediator = Substitute.For<IMediator>();
    }

    [Fact]
    public async Task Should_ReturnOkWithForecast_When_ForecastExists()
    {
        // Arrange
        var date = "2025-12-25";
        var response = new GetForecastQueryResponse(new DateOnly(2025, 12, 25), 5, "Snowy");
        
        _mediator.Send(Arg.Is<GetForecastQuery>(q => q.From == new DateOnly(2025, 12, 25)))
            .Returns(Result.Ok(response));

        // Act
        var result = await GetWeather.Handle(_mediator, date);

        // Assert
        result.Result.Should().BeOfType<Ok<GetWeatherResponse>>();
        var okResult = (Ok<GetWeatherResponse>)result.Result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Forecast.Date.Should().Be(new DateOnly(2025, 12, 25));
        okResult.Value.Forecast.TemperatureC.Should().Be(5);
        okResult.Value.Forecast.Summary.Should().Be("Snowy");
    }

    [Fact]
    public async Task Should_ReturnProblem_When_ForecastNotFound()
    {
        // Arrange
        var date = "2025-12-25";
        _mediator.Send(Arg.Any<GetForecastQuery>())
            .Returns(Result.Fail("Forecast not found"));

        // Act
        var result = await GetWeather.Handle(_mediator, date);

        // Assert
        result.Result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result.Result;
        problemResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Should_ReturnProblem_When_MultipleErrors()
    {
        // Arrange
        var date = "2025-12-25";
        _mediator.Send(Arg.Any<GetForecastQuery>())
            .Returns(Result.Fail(new Error("Error 1")).WithError("Error 2"));

        // Act
        var result = await GetWeather.Handle(_mediator, date);

        // Assert
        result.Result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result.Result;
        problemResult.ProblemDetails.Detail.Should().Contain("Error 1");
        problemResult.ProblemDetails.Detail.Should().Contain("Error 2");
    }

    [Fact]
    public async Task Should_ParseDateCorrectly_When_ValidDateProvided()
    {
        // Arrange
        var date = "2026-01-15";
        var expectedDate = new DateOnly(2026, 1, 15);
        var response = new GetForecastQueryResponse(expectedDate, 20, "Warm");
        
        _mediator.Send(Arg.Is<GetForecastQuery>(q => q.From == expectedDate))
            .Returns(Result.Ok(response));

        // Act
        await GetWeather.Handle(_mediator, date);

        // Assert
        await _mediator.Received(1).Send(Arg.Is<GetForecastQuery>(q => q.From == expectedDate));
    }

    [Fact]
    public async Task Should_CallMediatorOnce_When_HandleCalled()
    {
        // Arrange
        var date = "2025-12-25";
        var response = new GetForecastQueryResponse(new DateOnly(2025, 12, 25), 5, "Snowy");
        _mediator.Send(Arg.Any<GetForecastQuery>())
            .Returns(Result.Ok(response));

        // Act
        await GetWeather.Handle(_mediator, date);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<GetForecastQuery>());
    }

    [Fact]
    public async Task Should_ReturnForecastWithNullSummary_When_SummaryIsNull()
    {
        // Arrange
        var date = "2025-12-25";
        var response = new GetForecastQueryResponse(new DateOnly(2025, 12, 25), 10, null);
        
        _mediator.Send(Arg.Any<GetForecastQuery>())
            .Returns(Result.Ok(response));

        // Act
        var result = await GetWeather.Handle(_mediator, date);

        // Assert
        result.Result.Should().BeOfType<Ok<GetWeatherResponse>>();
        var okResult = (Ok<GetWeatherResponse>)result.Result;
        okResult.Value!.Forecast.Summary.Should().BeNull();
    }
}
