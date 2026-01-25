using Application;
using Application.Weather.AddForecast;
using AugmentService.Core;
using AugmentService.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AugmentService.UnitTests.Application;

public class AddForecastCommandHandlerTests
{
    private readonly IWeatherRepository _weatherRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AddForecastCommandHandler _sut;

    public AddForecastCommandHandlerTests()
    {
        _weatherRepository = Substitute.For<IWeatherRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new AddForecastCommandHandler(_weatherRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenForecastDoesNotExist_AddsAndReturnsSuccess()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var command = new AddForecastCommand(date, 25, "Sunny");

        _weatherRepository.GetForecastAsync(date)
            .Returns(Task.FromResult<Forecast?>(null));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _weatherRepository.Received(1).GetForecastAsync(date);
        await _weatherRepository.Received(1).AddForecastAsync(Arg.Is<Forecast>(f => 
            f.Date == date && 
            f.TemperatureC == 25 && 
            f.Summary == "Sunny"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenForecastAlreadyExists_ReturnsFailure()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var command = new AddForecastCommand(date, 25, "Sunny");
        var existingForecast = Forecast.New(date, 20, "Cloudy").Value;

        _weatherRepository.GetForecastAsync(date)
            .Returns(Task.FromResult<Forecast?>(existingForecast));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("Forecast already exists");
        await _weatherRepository.DidNotReceive().AddForecastAsync(Arg.Any<Forecast>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTemperatureIsInvalid_ReturnsFailure()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var command = new AddForecastCommand(date, 100, "Too Hot"); // Invalid: > 60

        _weatherRepository.GetForecastAsync(date)
            .Returns(Task.FromResult<Forecast?>(null));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("Temperature must be between -90 and 60");
        await _weatherRepository.DidNotReceive().AddForecastAsync(Arg.Any<Forecast>());
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsException_ReturnsFailure()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var command = new AddForecastCommand(date, 25, "Sunny");

        _weatherRepository.GetForecastAsync(date)
            .Returns(Task.FromResult<Forecast?>(null));
        _weatherRepository.AddForecastAsync(Arg.Any<Forecast>())
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("Failed to add forecast");
    }

    [Fact]
    public async Task Handle_WithNullSummary_AddsSuccessfully()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var command = new AddForecastCommand(date, 25, null);

        _weatherRepository.GetForecastAsync(date)
            .Returns(Task.FromResult<Forecast?>(null));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _weatherRepository.Received(1).AddForecastAsync(Arg.Is<Forecast>(f => f.Summary == null));
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(0)]
    [InlineData(60)]
    public async Task Handle_WithBoundaryTemperatures_AddsSuccessfully(int temperature)
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Today);
        var command = new AddForecastCommand(date, temperature, "Boundary test");

        _weatherRepository.GetForecastAsync(date)
            .Returns(Task.FromResult<Forecast?>(null));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
