using Dapr.Workflow;
using AugmentService.Api.Activities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AugmentService.Api.UnitTests.Activities;

public class NotifyActivityTests
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly NotifyActivity _activity;

    public NotifyActivityTests()
    {
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _activity = new NotifyActivity(_loggerFactory);
    }

    private static WorkflowActivityContext MakeContext() =>
        Substitute.For<WorkflowActivityContext>();

    [Fact]
    public async Task RunAsync_Should_ReturnNull_When_Called()
    {
        // Arrange
        var notification = new Notification("Order received");

        // Act
        var result = await _activity.RunAsync(MakeContext(), notification);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_Should_Complete_When_MessageIsEmpty()
    {
        // Arrange
        var notification = new Notification(string.Empty);

        // Act
        var act = async () => await _activity.RunAsync(MakeContext(), notification);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("Order ORD-001 has been received")]
    [InlineData("Payment processed successfully")]
    [InlineData("Inventory reserved for order")]
    public async Task RunAsync_Should_ReturnNull_ForVariousMessages(string message)
    {
        // Arrange
        var notification = new Notification(message);

        // Act
        var result = await _activity.RunAsync(MakeContext(), notification);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Constructor_Should_CreateLogger_Via_LoggerFactory()
    {
        // Act
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _ = new NotifyActivity(loggerFactory);

        // Assert
        loggerFactory.Received(1).CreateLogger(Arg.Any<string>());
    }
}
