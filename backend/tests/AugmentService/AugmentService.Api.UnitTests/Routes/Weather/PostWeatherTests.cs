using AugmentService.Api.Routes.Weather.Endpoints;
using AugmentService.Api.Routes.Weather.Models;
using Application.Weather.AddForecast;
using FluentResults;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using Xunit;

namespace AugmentService.Api.UnitTests.Routes.Weather;

public class PostWeatherTests
{
    private readonly IMediator _mediator;
    private readonly PostWeatherRequestValidator _validator;

    public PostWeatherTests()
    {
        _mediator = Substitute.For<IMediator>();
        _validator = new PostWeatherRequestValidator();
    }

    [Fact]
    public async Task Should_ReturnCreated_When_ValidRequestProvided()
    {
        // Arrange
        var request = new PostWeatherRequest(
            DateTimeOffset.UtcNow.AddDays(1),
            25,
            "Sunny"
        );

        _mediator.Send(Arg.Any<AddForecastCommand>())
            .Returns(Result.Ok());

        // Act
        var result = await PostWeather.Handle(_mediator, _validator, request);

        // Assert
        result.Result.Should().BeOfType<Created>();
    }

    [Fact]
    public async Task Should_ReturnValidationProblem_When_DateIsInPast()
    {
        // Arrange
        var request = new PostWeatherRequest(
            DateTimeOffset.UtcNow.AddDays(-1), // Past date
            25,
            "Sunny"
        );

        // Act
        var result = await PostWeather.Handle(_mediator, _validator, request);

        // Assert
        result.Result.Should().BeOfType<ValidationProblem>();
    }

    [Fact]
    public async Task Should_ReturnValidationProblem_When_TemperatureTooHigh()
    {
        // Arrange
        var request = new PostWeatherRequest(
            DateTimeOffset.UtcNow.AddDays(1),
            65, // Above 60°C limit
            "Too Hot"
        );

        // Act
        var result = await PostWeather.Handle(_mediator, _validator, request);

        // Assert
        result.Result.Should().BeOfType<ValidationProblem>();
    }

    [Fact]
    public async Task Should_ReturnValidationProblem_When_TemperatureTooLow()
    {
        // Arrange
        var request = new PostWeatherRequest(
            DateTimeOffset.UtcNow.AddDays(1),
            -95, // Below -90°C limit
            "Too Cold"
        );

        // Act
        var result = await PostWeather.Handle(_mediator, _validator, request);

        // Assert
        result.Result.Should().BeOfType<ValidationProblem>();
    }

    [Fact]
    public async Task Should_AcceptValidTemperatureRange_When_TemperatureWithinBounds()
    {
        // Arrange
        var request = new PostWeatherRequest(
            DateTimeOffset.UtcNow.AddDays(1),
            -90, // Minimum valid
            "Very Cold"
        );

        _mediator.Send(Arg.Any<AddForecastCommand>())
            .Returns(Result.Ok());

        // Act
        var result = await PostWeather.Handle(_mediator, _validator, request);

        // Assert
        result.Result.Should().BeOfType<Created>();
    }

    [Fact]
    public async Task Should_AcceptMaxValidTemperature_When_Temperature60C()
    {
        // Arrange
        var request = new PostWeatherRequest(
            DateTimeOffset.UtcNow.AddDays(1),
            60, // Maximum valid
            "Very Hot"
        );

        _mediator.Send(Arg.Any<AddForecastCommand>())
            .Returns(Result.Ok());

        // Act
        var result = await PostWeather.Handle(_mediator, _validator, request);

        // Assert
        result.Result.Should().BeOfType<Created>();
    }

    [Fact]
    public async Task Should_ReturnProblem_When_MediatorReturnsFailure()
    {
        // Arrange
        var request = new PostWeatherRequest(
            DateTimeOffset.UtcNow.AddDays(1),
            25,
            "Sunny"
        );

        _mediator.Send(Arg.Any<AddForecastCommand>())
            .Returns(Result.Fail("Database error"));

        // Act
        var result = await PostWeather.Handle(_mediator, _validator, request);

        // Assert
        result.Result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result.Result;
        problemResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Should_ConvertDateTimeOffsetToDateOnly_When_SendingCommand()
    {
        // Arrange
        var futureDate = DateTimeOffset.UtcNow.AddDays(10);
        var expectedDate = new DateOnly(futureDate.Year, futureDate.Month, futureDate.Day);
        var request = new PostWeatherRequest(futureDate, 5, "Christmas");

        AddForecastCommand? capturedCommand = null;
        _mediator.Send(Arg.Any<AddForecastCommand>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedCommand = callInfo.Arg<AddForecastCommand>();
                return Result.Ok();
            });

        // Act
        await PostWeather.Handle(_mediator, _validator, request);

        // Assert
        capturedCommand.Should().NotBeNull();
        capturedCommand!.Date.Should().Be(expectedDate);
        capturedCommand.TemperatureC.Should().Be(5);
        capturedCommand.Summary.Should().Be("Christmas");
    }

    [Fact]
    public async Task Should_AcceptNullSummary_When_SummaryNotProvided()
    {
        // Arrange
        var request = new PostWeatherRequest(
            DateTimeOffset.UtcNow.AddDays(1),
            20,
            null
        );

        _mediator.Send(Arg.Any<AddForecastCommand>())
            .Returns(Result.Ok());

        // Act
        var result = await PostWeather.Handle(_mediator, _validator, request);

        // Assert
        result.Result.Should().BeOfType<Created>();
        await _mediator.Received(1).Send(Arg.Is<AddForecastCommand>(cmd => cmd.Summary == null));
    }

    [Fact]
    public async Task Should_CallMediatorOnce_When_ValidationPasses()
    {
        // Arrange
        var request = new PostWeatherRequest(
            DateTimeOffset.UtcNow.AddDays(1),
            25,
            "Sunny"
        );

        _mediator.Send(Arg.Any<AddForecastCommand>())
            .Returns(Result.Ok());

        // Act
        await PostWeather.Handle(_mediator, _validator, request);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<AddForecastCommand>());
    }

    [Fact]
    public async Task Should_NotCallMediator_When_ValidationFails()
    {
        // Arrange
        var request = new PostWeatherRequest(
            DateTimeOffset.UtcNow.AddDays(-1), // Invalid
            25,
            "Sunny"
        );

        // Act
        await PostWeather.Handle(_mediator, _validator, request);

        // Assert
        await _mediator.DidNotReceive().Send(Arg.Any<AddForecastCommand>());
    }

    [Fact]
    public async Task Should_AcceptTodaysDate_When_DateEqualsToday()
    {
        // Arrange
        var request = new PostWeatherRequest(
            DateTimeOffset.UtcNow,
            22,
            "Today"
        );

        _mediator.Send(Arg.Any<AddForecastCommand>())
            .Returns(Result.Ok());

        // Act
        var result = await PostWeather.Handle(_mediator, _validator, request);

        // Assert
        result.Result.Should().BeOfType<Created>();
    }
}
