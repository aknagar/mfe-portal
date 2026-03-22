using Application;
using FluentAssertions;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using NSubstitute;
using Xunit;

namespace AugmentService.Application.UnitTests;

public class LoggingBehaviorTests
{
    private readonly Mock<ILogger<LoggingBehavior<TestRequest, Result>>> _loggerMock;
    private readonly RequestHandlerDelegate<Result> _next;
    private readonly LoggingBehavior<TestRequest, Result> _sut;

    public LoggingBehaviorTests()
    {
        _loggerMock = new Mock<ILogger<LoggingBehavior<TestRequest, Result>>>();
        _next = Substitute.For<RequestHandlerDelegate<Result>>();
        _sut = new LoggingBehavior<TestRequest, Result>(_loggerMock.Object);
    }

    [Fact]
    public async Task Should_LogInformationBeforeRequest_When_HandleCalled()
    {
        // Arrange
        var request = new TestRequest();
        _next().Returns(Result.Ok());

        // Act
        await _sut.Handle(request, _next, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Start request") && v.ToString()!.Contains("TestRequest")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_LogInformationAfterSuccess_When_RequestSucceeds()
    {
        // Arrange
        var request = new TestRequest();
        _next().Returns(Result.Ok());

        // Act
        await _sut.Handle(request, _next, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Completed request") && v.ToString()!.Contains("TestRequest")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_LogErrorAfterFailure_When_RequestFails()
    {
        // Arrange
        var request = new TestRequest();
        var error = new Error("Something went wrong");
        var failedResult = Result.Fail(error);
        _next().Returns(failedResult);

        // Act
        await _sut.Handle(request, _next, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("TestRequest") && v.ToString()!.Contains("failed")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_ReturnSuccessResult_When_NextHandlerSucceeds()
    {
        // Arrange
        var request = new TestRequest();
        var successResult = Result.Ok();
        _next().Returns(successResult);

        // Act
        var result = await _sut.Handle(request, _next, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.IsFailed.Should().BeFalse();
    }

    [Fact]
    public async Task Should_ReturnFailedResult_When_NextHandlerFails()
    {
        // Arrange
        var request = new TestRequest();
        var error = new Error("Handler error");
        var failedResult = Result.Fail(error);
        _next().Returns(failedResult);

        // Act
        var result = await _sut.Handle(request, _next, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailed.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Message.Should().Be("Handler error");
    }

    [Fact]
    public async Task Should_CallNextHandler_When_HandleCalled()
    {
        // Arrange
        var request = new TestRequest();
        _next().Returns(Result.Ok());

        // Act
        await _sut.Handle(request, _next, CancellationToken.None);

        // Assert
        await _next.Received(1)();
    }

    [Fact]
    public async Task Should_LogTwoMessages_When_RequestSucceeds()
    {
        // Arrange
        var request = new TestRequest();
        _next().Returns(Result.Ok());

        // Act
        await _sut.Handle(request, _next, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Should_LogTwoMessages_When_RequestFails()
    {
        // Arrange
        var request = new TestRequest();
        _next().Returns(Result.Fail("Error"));

        // Act
        await _sut.Handle(request, _next, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Should_NotLogCompletedMessage_When_RequestFails()
    {
        // Arrange
        var request = new TestRequest();
        _next().Returns(Result.Fail("Error"));

        // Act
        await _sut.Handle(request, _next, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Completed request")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_NotLogErrorMessage_When_RequestSucceeds()
    {
        // Arrange
        var request = new TestRequest();
        _next().Returns(Result.Ok());

        // Act
        await _sut.Handle(request, _next, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_HandleMultipleErrors_When_RequestFailsWithMultipleErrors()
    {
        // Arrange
        var request = new TestRequest();
        var failedResult = Result.Fail("Error 1").WithError("Error 2").WithError("Error 3");
        _next().Returns(failedResult);

        // Act
        var result = await _sut.Handle(request, _next, CancellationToken.None);

        // Assert
        result.Errors.Should().HaveCount(3);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("TestRequest") && v.ToString()!.Contains("failed")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_PreserveResultData_When_ResultHasValue()
    {
        // Arrange
        var request = new TestRequest();
        var successResult = Result.Ok().WithSuccess("Operation completed");
        _next().Returns(successResult);

        // Act
        var result = await _sut.Handle(request, _next, CancellationToken.None);

        // Assert
        result.Successes.Should().HaveCount(1);
        result.Successes[0].Message.Should().Be("Operation completed");
    }

    [Fact]
    public async Task Should_UseCorrectRequestName_When_DifferentRequestTypes()
    {
        // Arrange
        var logger = new Mock<ILogger<LoggingBehavior<AnotherTestRequest, Result>>>();
        var next = Substitute.For<RequestHandlerDelegate<Result>>();
        var behavior = new LoggingBehavior<AnotherTestRequest, Result>(logger.Object);
        var request = new AnotherTestRequest();
        next().Returns(Result.Ok());

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Start request") && v.ToString()!.Contains("AnotherTestRequest")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_RespectCancellationToken_When_TokenProvided()
    {
        // Arrange
        var request = new TestRequest();
        var cts = new CancellationTokenSource();
        _next().Returns(Result.Ok());

        // Act
        await _sut.Handle(request, _next, cts.Token);

        // Assert
        await _next.Received(1)();
    }
}

// Test request types
public record TestRequest : IRequest<Result>;
public record AnotherTestRequest : IRequest<Result>;
