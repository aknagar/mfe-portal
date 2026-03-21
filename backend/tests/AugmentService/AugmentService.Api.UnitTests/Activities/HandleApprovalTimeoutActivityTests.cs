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

public class HandleApprovalTimeoutActivityTests
{
    private readonly DaprClient _daprClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly HandleApprovalTimeoutActivity _activity;

    public HandleApprovalTimeoutActivityTests()
    {
        _daprClient = Substitute.For<DaprClient>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _activity = new HandleApprovalTimeoutActivity(_loggerFactory, _daprClient);
    }

    private static WorkflowActivityContext MakeContext() =>
        Substitute.For<WorkflowActivityContext>();

    [Fact]
    public async Task RunAsync_Should_ReturnFailure_When_ApprovalNotFound()
    {
        // Arrange
        var payload = new ApprovalTimeoutPayload("ORD-001");
        _daprClient
            .GetStateAsync<ApprovalRequest>("statestore", "approval_ORD-001",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns((ApprovalRequest?)null);

        // Act
        var result = await _activity.RunAsync(MakeContext(), payload);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Approval already processed or not found");
    }

    [Fact]
    public async Task RunAsync_Should_ReturnFailure_When_ApprovalAlreadyProcessed()
    {
        // Arrange
        var payload = new ApprovalTimeoutPayload("ORD-002");
        var approval = new ApprovalRequest
        {
            OrderId = "ORD-002",
            OrderName = "Widget",
            Status = ApprovalStatus.Approved,   // already processed
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };
        _daprClient
            .GetStateAsync<ApprovalRequest>("statestore", "approval_ORD-002",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(approval);

        // Act
        var result = await _activity.RunAsync(MakeContext(), payload);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Approval already processed or not found");
    }

    [Fact]
    public async Task RunAsync_Should_ReturnFailure_And_UpdateStatus_When_ApprovalPending()
    {
        // Arrange
        var payload = new ApprovalTimeoutPayload("ORD-003");
        var approval = new ApprovalRequest
        {
            OrderId = "ORD-003",
            OrderName = "Gadget",
            Status = ApprovalStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };
        _daprClient
            .GetStateAsync<ApprovalRequest>("statestore", "approval_ORD-003",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(approval);

        // Act
        var result = await _activity.RunAsync(MakeContext(), payload);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Approval timed out");
    }

    [Fact]
    public async Task RunAsync_Should_SetStatusToTimedOut_When_ApprovalIsPending()
    {
        // Arrange
        var payload = new ApprovalTimeoutPayload("ORD-004");
        var approval = new ApprovalRequest
        {
            OrderId = "ORD-004",
            OrderName = "Thing",
            Status = ApprovalStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };
        _daprClient
            .GetStateAsync<ApprovalRequest>("statestore", "approval_ORD-004",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(approval);

        // Act
        await _activity.RunAsync(MakeContext(), payload);

        // Assert — SaveStateAsync called with TimedOut status
        await _daprClient.Received(1).SaveStateAsync(
            "statestore",
            "approval_ORD-004",
            Arg.Is<ApprovalRequest>(r => r.Status == ApprovalStatus.TimedOut),
            Arg.Any<StateOptions?>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_Should_SetTimeoutComment_When_ApprovalIsPending()
    {
        // Arrange
        var payload = new ApprovalTimeoutPayload("ORD-005");
        var approval = new ApprovalRequest
        {
            OrderId = "ORD-005",
            OrderName = "Product",
            Status = ApprovalStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };
        ApprovalRequest? saved = null;
        _daprClient
            .GetStateAsync<ApprovalRequest>("statestore", "approval_ORD-005",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(approval);
        await _daprClient.SaveStateAsync(
            "statestore",
            Arg.Any<string>(),
            Arg.Do<ApprovalRequest>(r => saved = r),
            Arg.Any<StateOptions?>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>());

        // Act
        await _activity.RunAsync(MakeContext(), payload);

        // Assert
        saved.Should().NotBeNull();
        saved!.Comments.Should().Be("Approval request timed out after 24 hours");
        saved.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RunAsync_Should_NotSaveState_When_ApprovalNotFound()
    {
        // Arrange
        var payload = new ApprovalTimeoutPayload("ORD-006");
        _daprClient
            .GetStateAsync<ApprovalRequest>("statestore", "approval_ORD-006",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns((ApprovalRequest?)null);

        // Act
        await _activity.RunAsync(MakeContext(), payload);

        // Assert
        await _daprClient.DidNotReceive().SaveStateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ApprovalRequest>(),
            Arg.Any<StateOptions?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_Should_UseCorrectStateKey()
    {
        // Arrange
        var payload = new ApprovalTimeoutPayload("MY-ORDER-77");
        _daprClient
            .GetStateAsync<ApprovalRequest>("statestore", "approval_MY-ORDER-77",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns((ApprovalRequest?)null);

        // Act
        await _activity.RunAsync(MakeContext(), payload);

        // Assert
        await _daprClient.Received(1).GetStateAsync<ApprovalRequest>(
            "statestore",
            "approval_MY-ORDER-77",
            Arg.Any<ConsistencyMode?>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>());
    }
}
