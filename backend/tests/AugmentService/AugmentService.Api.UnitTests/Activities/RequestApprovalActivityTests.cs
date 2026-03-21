using Dapr.Client;
using Dapr.Workflow;
using AugmentService.Api.Activities;
using AugmentService.Api.Models;
using AugmentService.Core.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AugmentService.Api.UnitTests.Activities;

public class RequestApprovalActivityTests
{
    private readonly DaprClient _daprClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly RequestApprovalActivity _activity;

    public RequestApprovalActivityTests()
    {
        _daprClient = Substitute.For<DaprClient>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _activity = new RequestApprovalActivity(_loggerFactory, _daprClient);
    }

    private static WorkflowActivityContext MakeContext() =>
        Substitute.For<WorkflowActivityContext>();

    [Fact]
    public async Task RunAsync_Should_ReturnSuccess_When_ApprovalRequestCreated()
    {
        // Arrange
        var payload = new ApprovalPayload("ORD-001", "Widget", 1500.00, 3);

        // Act
        var result = await _activity.RunAsync(MakeContext(), payload);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Approval request created");
    }

    [Fact]
    public async Task RunAsync_Should_SaveApprovalToStateStore()
    {
        // Arrange
        var payload = new ApprovalPayload("ORD-002", "Gadget", 2000.00, 1);

        // Act
        await _activity.RunAsync(MakeContext(), payload);

        // Assert — state was saved with the correct key
        await _daprClient.Received(1).SaveStateAsync(
            "statestore",
            "approval_ORD-002",
            Arg.Is<ApprovalRequest>(r =>
                r.OrderId == "ORD-002" &&
                r.OrderName == "Gadget" &&
                r.Status == ApprovalStatus.Pending),
            Arg.Any<StateOptions?>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_Should_CreateApprovalWithPendingStatus()
    {
        // Arrange
        var payload = new ApprovalPayload("ORD-003", "Thing", 500.00, 2);
        ApprovalRequest? captured = null;
        await _daprClient.SaveStateAsync(
            "statestore",
            Arg.Any<string>(),
            Arg.Do<ApprovalRequest>(r => captured = r),
            Arg.Any<StateOptions?>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>());

        // Act
        await _activity.RunAsync(MakeContext(), payload);

        // Assert
        captured.Should().NotBeNull();
        captured!.Status.Should().Be(ApprovalStatus.Pending);
    }

    [Fact]
    public async Task RunAsync_Should_SetExpiresAtTo24HoursFromNow()
    {
        // Arrange
        var payload = new ApprovalPayload("ORD-004", "Item", 100.00, 1);
        var before = DateTime.UtcNow;
        ApprovalRequest? captured = null;
        await _daprClient.SaveStateAsync(
            "statestore",
            Arg.Any<string>(),
            Arg.Do<ApprovalRequest>(r => captured = r),
            Arg.Any<StateOptions?>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>());

        // Act
        await _activity.RunAsync(MakeContext(), payload);
        var after = DateTime.UtcNow;

        // Assert
        captured.Should().NotBeNull();
        captured!.ExpiresAt.Should().BeOnOrAfter(before.AddHours(24));
        captured.ExpiresAt.Should().BeOnOrBefore(after.AddHours(24).AddSeconds(1));
    }

    [Fact]
    public async Task RunAsync_Should_UseCorrectStateKeyPrefix()
    {
        // Arrange
        var payload = new ApprovalPayload("MY-ORDER-99", "Product", 999.00, 5);

        // Act
        await _activity.RunAsync(MakeContext(), payload);

        // Assert — key must be "approval_MY-ORDER-99"
        await _daprClient.Received(1).SaveStateAsync(
            "statestore",
            "approval_MY-ORDER-99",
            Arg.Any<ApprovalRequest>(),
            Arg.Any<StateOptions?>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_Should_PropagateDaprException_When_SaveStateFails()
    {
        // Arrange
        var payload = new ApprovalPayload("ORD-005", "Widget", 1000.00, 1);
        _daprClient
            .When(c => c.SaveStateAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ApprovalRequest>(),
                Arg.Any<StateOptions?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("State store unavailable"));

        // Act
        Func<Task> act = async () => await _activity.RunAsync(MakeContext(), payload);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*State store unavailable*");
    }

    [Fact]
    public void Constructor_Should_CreateLogger_Via_LoggerFactory()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var daprClient = Substitute.For<DaprClient>();

        _ = new RequestApprovalActivity(loggerFactory, daprClient);

        loggerFactory.Received(1).CreateLogger(Arg.Any<string>());
    }
}
