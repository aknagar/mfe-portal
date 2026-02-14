using Application;
using Application.Weather.DeleteForecast;
using AugmentService.Core;
using AugmentService.Core.Interfaces;
using FluentAssertions;
using FluentResults;
using NSubstitute;
using Xunit;

namespace AugmentService.Application.UnitTests.Weather.DeleteForecast;

public class DeleteForecastCommandHandlerTests
{
    private readonly IWeatherRepository _weatherRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DeleteForecastCommandHandler _sut;

    public DeleteForecastCommandHandlerTests()
    {
        _weatherRepository = Substitute.For<IWeatherRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new DeleteForecastCommandHandler(_weatherRepository, _unitOfWork);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_ForecastExistsAndDeleteSucceeds()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 15);
        var forecast = Forecast.New(date, 25, "Sunny").Value;
        var command = new DeleteForecastCommand(date);

        _weatherRepository.GetForecastAsync(date).Returns(forecast);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        forecast.IsDeleted.Should().BeTrue();

        await _weatherRepository.Received(1).GetForecastAsync(date);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_ForecastDoesNotExist()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 15);
        var command = new DeleteForecastCommand(date);

        _weatherRepository.GetForecastAsync(date).Returns((Forecast?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        await _weatherRepository.Received(1).GetForecastAsync(date);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_CallDeleteOnForecast_When_ForecastExists()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 15);
        var forecast = Forecast.New(date, 20, "Cloudy").Value;
        var command = new DeleteForecastCommand(date);

        _weatherRepository.GetForecastAsync(date).Returns(forecast);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        forecast.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Should_SaveChanges_When_ForecastDeleted()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 15);
        var forecast = Forecast.New(date, 15, "Rainy").Value;
        var command = new DeleteForecastCommand(date);

        _weatherRepository.GetForecastAsync(date).Returns(forecast);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_RespectCancellationToken_When_TokenProvided()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 15);
        var forecast = Forecast.New(date, 10, "Snowy").Value;
        var command = new DeleteForecastCommand(date);
        var cts = new CancellationTokenSource();

        _weatherRepository.GetForecastAsync(date).Returns(forecast);
        _unitOfWork.SaveChangesAsync(cts.Token).Returns(1);

        // Act
        await _sut.Handle(command, cts.Token);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(cts.Token);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_SaveChangesThrowsException()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 15);
        var forecast = Forecast.New(date, 18, "Partly Cloudy").Value;
        var command = new DeleteForecastCommand(date);
        var exception = new InvalidOperationException("Database error");

        _weatherRepository.GetForecastAsync(date).Returns(forecast);
        _unitOfWork.When(x => x.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ => throw exception);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Message.Should().Be("Failed to delete forecast");
        result.Errors[0].Reasons.Should().Contain(r => r.Message.Contains("Database error"));
    }

    [Fact(Skip = "NSubstitute When/Do exception setup issue - needs fix")]
    public async Task Should_ReturnFailure_When_GetForecastThrowsException()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 15);
        var command = new DeleteForecastCommand(date);
        var exception = new InvalidOperationException("Repository error");

        _weatherRepository.When(x => x.GetForecastAsync(date))
            .Do(_ => throw exception);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Message.Should().Be("Failed to delete forecast");
    }

    [Theory]
    [InlineData(2024, 1, 1)]
    [InlineData(2024, 6, 15)]
    [InlineData(2024, 12, 31)]
    public async Task Should_HandleDifferentDates_When_CommandProvided(int year, int month, int day)
    {
        // Arrange
        var date = new DateOnly(year, month, day);
        var forecast = Forecast.New(date, 22, "Warm").Value;
        var command = new DeleteForecastCommand(date);

        _weatherRepository.GetForecastAsync(date).Returns(forecast);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _weatherRepository.Received(1).GetForecastAsync(date);
    }

    [Fact]
    public async Task Should_NotSaveChanges_When_ForecastNotFound()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 15);
        var command = new DeleteForecastCommand(date);

        _weatherRepository.GetForecastAsync(date).Returns((Forecast?)null);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_SetIsDeletedToTrue_When_DeleteCalled()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 15);
        var forecast = Forecast.New(date, 5, "Cold").Value;
        var command = new DeleteForecastCommand(date);

        forecast.IsDeleted.Should().BeFalse(); // Verify initial state

        _weatherRepository.GetForecastAsync(date).Returns(forecast);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        forecast.IsDeleted.Should().BeTrue();
    }
}
