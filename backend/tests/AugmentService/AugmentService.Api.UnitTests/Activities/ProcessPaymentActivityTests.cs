using Dapr.Client;
using Dapr.Workflow;
using AugmentService.Api.Activities;
using AugmentService.Api.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AugmentService.Api.UnitTests.Activities;

public class ProcessPaymentActivityTests
{
    private readonly DaprClient _daprClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ProcessPaymentActivity _activity;

    public ProcessPaymentActivityTests()
    {
        _daprClient = Substitute.For<DaprClient>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _activity = new ProcessPaymentActivity(_loggerFactory, _daprClient);
    }

    private static WorkflowActivityContext MakeContext() =>
        Substitute.For<WorkflowActivityContext>();

    [Fact]
    public async Task RunAsync_Should_ReturnNull_When_PaymentProcessed()
    {
        // Arrange
        var req = new PaymentRequest("req-001", "Widget", 2, 19.99);

        // Act
        var result = await _activity.RunAsync(MakeContext(), req);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_Should_Complete_WithoutCallingDaprClient()
    {
        // Arrange
        var req = new PaymentRequest("req-002", "Gadget", 1, 49.99);

        // Act
        await _activity.RunAsync(MakeContext(), req);

        // Assert — ProcessPaymentActivity does NOT read/write state
        await _daprClient.DidNotReceive().GetStateAsync<object>(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<ConsistencyMode?>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("order-A", "Widget", 1, 10.0)]
    [InlineData("order-B", "Gadget", 5, 250.0)]
    [InlineData("order-C", "SuperThing", 100, 9999.0)]
    public async Task RunAsync_Should_ReturnNull_ForVariousPayments(
        string requestId, string item, int amount, double currency)
    {
        // Arrange
        var req = new PaymentRequest(requestId, item, amount, currency);

        // Act
        var result = await _activity.RunAsync(MakeContext(), req);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Constructor_Should_CreateLogger_Via_LoggerFactory()
    {
        // Act
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var daprClient = Substitute.For<DaprClient>();

        _ = new ProcessPaymentActivity(loggerFactory, daprClient);

        // Assert
        loggerFactory.Received(1).CreateLogger(Arg.Any<string>());
    }
}
