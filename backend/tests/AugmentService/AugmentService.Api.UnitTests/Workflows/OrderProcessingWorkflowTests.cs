using Dapr.Workflow;
using DurableTask.Core.Exceptions;
using AugmentService.Api.Activities;
using AugmentService.Api.Controllers;
using AugmentService.Api.Models;
using AugmentService.Api.Workflows;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AugmentService.Api.UnitTests.Workflows;

/// <summary>
/// Unit tests for <see cref="OrderProcessingWorkflow"/>.
/// WorkflowContext is abstract so NSubstitute can substitute it directly.
/// Each test constructs the workflow and calls RunAsync directly — no Dapr runtime needed.
/// </summary>
public class OrderProcessingWorkflowTests
{
    private readonly WorkflowContext _context;
    private readonly OrderProcessingWorkflow _workflow;

    public OrderProcessingWorkflowTests()
    {
        _context = Substitute.For<WorkflowContext>();
        _context.InstanceId.Returns("test-instance-id");

        // Default: non-generic CallActivityAsync (used by Notify/RequestApproval/ProcessPayment etc.)
        // returns a completed task — most tests don't care about notification side-effects.
        _context.CallActivityAsync(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<WorkflowTaskOptions?>())
            .Returns(Task.CompletedTask);

        _workflow = new OrderProcessingWorkflow();
    }

    #region Successful low-cost order (no approval required)

    [Fact]
    public async Task RunAsync_Should_ReturnProcessedTrue_When_InventorySufficientAndCostBelowThreshold()
    {
        // Arrange — 5 in stock, requesting 2, cost $500 (below $1000 threshold)
        var order = new OrderPayload("Widget", TotalCost: 500.0, Quantity: 2);
        var inventoryResult = new InventoryResult(Success: true, orderPayload: new OrderPayload("Widget", 500.0, 5));

        _context.CallActivityAsync<InventoryResult>(
                nameof(ReserveInventoryActivity),
                Arg.Any<object?>(),
                Arg.Any<WorkflowTaskOptions?>())
            .Returns(inventoryResult);

        // Act
        var result = await _workflow.RunAsync(_context, order);

        // Assert
        result.Processed.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_Should_CallProcessPaymentActivity_When_OrderSucceeds()
    {
        // Arrange
        var order = new OrderPayload("Widget", TotalCost: 100.0, Quantity: 1);
        var inventoryResult = new InventoryResult(Success: true, orderPayload: new OrderPayload("Widget", 100.0, 10));

        _context.CallActivityAsync<InventoryResult>(
                nameof(ReserveInventoryActivity),
                Arg.Any<object?>(),
                Arg.Any<WorkflowTaskOptions?>())
            .Returns(inventoryResult);

        // Act
        await _workflow.RunAsync(_context, order);

        // Assert — ProcessPaymentActivity must be called
        await _context.Received(1).CallActivityAsync(
            nameof(ProcessPaymentActivity),
            Arg.Any<object?>(),
            Arg.Any<WorkflowTaskOptions?>());
    }

    [Fact]
    public async Task RunAsync_Should_CallUpdateInventoryActivity_When_OrderSucceeds()
    {
        // Arrange
        var order = new OrderPayload("Widget", TotalCost: 100.0, Quantity: 1);
        var inventoryResult = new InventoryResult(Success: true, orderPayload: new OrderPayload("Widget", 100.0, 10));

        _context.CallActivityAsync<InventoryResult>(
                nameof(ReserveInventoryActivity),
                Arg.Any<object?>(),
                Arg.Any<WorkflowTaskOptions?>())
            .Returns(inventoryResult);

        // Act
        await _workflow.RunAsync(_context, order);

        // Assert — UpdateInventoryActivity must be called
        await _context.Received(1).CallActivityAsync(
            nameof(UpdateInventoryActivity),
            Arg.Any<object?>(),
            Arg.Any<WorkflowTaskOptions?>());
    }

    #endregion

    #region Insufficient inventory

    [Fact]
    public async Task RunAsync_Should_ReturnProcessedFalse_When_InsufficientInventory()
    {
        // Arrange
        var order = new OrderPayload("Widget", TotalCost: 100.0, Quantity: 5);
        var inventoryResult = new InventoryResult(Success: false, orderPayload: null);

        _context.CallActivityAsync<InventoryResult>(
                nameof(ReserveInventoryActivity),
                Arg.Any<object?>(),
                Arg.Any<WorkflowTaskOptions?>())
            .Returns(inventoryResult);

        // Act
        var result = await _workflow.RunAsync(_context, order);

        // Assert
        result.Processed.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_Should_NotCallProcessPaymentActivity_When_InsufficientInventory()
    {
        // Arrange
        var order = new OrderPayload("Widget", TotalCost: 100.0, Quantity: 5);
        var inventoryResult = new InventoryResult(Success: false, orderPayload: null);

        _context.CallActivityAsync<InventoryResult>(
                nameof(ReserveInventoryActivity),
                Arg.Any<object?>(),
                Arg.Any<WorkflowTaskOptions?>())
            .Returns(inventoryResult);

        // Act
        await _workflow.RunAsync(_context, order);

        // Assert — payment must NOT be processed if inventory is insufficient
        await _context.DidNotReceive().CallActivityAsync<object>(
            nameof(ProcessPaymentActivity),
            Arg.Any<object?>(),
            Arg.Any<WorkflowTaskOptions?>());
    }

    #endregion

    #region High-cost order — approval required

    [Fact]
    public async Task RunAsync_Should_ReturnProcessedTrue_When_HighCostOrderApproved()
    {
        // Arrange — $1500 order (above $1000 threshold) — approved
        var order = new OrderPayload("Laptop", TotalCost: 1500.0, Quantity: 1);
        var inventoryResult = new InventoryResult(Success: true, orderPayload: new OrderPayload("Laptop", 1500.0, 5));
        var approvalDecision = new ApprovalDecision(
            IsApproved: true, ApprovedBy: "manager@test.com", Comments: "Approved");

        _context.CallActivityAsync<InventoryResult>(
                nameof(ReserveInventoryActivity),
                Arg.Any<object?>(),
                Arg.Any<WorkflowTaskOptions?>())
            .Returns(inventoryResult);

        // Timer never completes (approval arrives first)
        var neverCompletingTimer = new TaskCompletionSource<object>().Task;
        _context.CreateTimer(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(neverCompletingTimer);

        _context.WaitForExternalEventAsync<ApprovalDecision>(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(approvalDecision));

        // Act
        var result = await _workflow.RunAsync(_context, order);

        // Assert
        result.Processed.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_Should_ReturnProcessedFalse_When_HighCostOrderRejected()
    {
        // Arrange — $1500 order — rejected
        var order = new OrderPayload("Laptop", TotalCost: 1500.0, Quantity: 1);
        var inventoryResult = new InventoryResult(Success: true, orderPayload: new OrderPayload("Laptop", 1500.0, 5));
        var rejectionDecision = new ApprovalDecision(
            IsApproved: false, ApprovedBy: "manager@test.com", Comments: "Too expensive");

        _context.CallActivityAsync<InventoryResult>(
                nameof(ReserveInventoryActivity),
                Arg.Any<object?>(),
                Arg.Any<WorkflowTaskOptions?>())
            .Returns(inventoryResult);

        var neverCompletingTimer = new TaskCompletionSource<object>().Task;
        _context.CreateTimer(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(neverCompletingTimer);

        _context.WaitForExternalEventAsync<ApprovalDecision>(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(rejectionDecision));

        // Act
        var result = await _workflow.RunAsync(_context, order);

        // Assert
        result.Processed.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_Should_ReturnProcessedFalse_When_ApprovalTimesOut()
    {
        // Arrange — $1500 order — timer fires before approval event arrives
        var order = new OrderPayload("Laptop", TotalCost: 1500.0, Quantity: 1);
        var inventoryResult = new InventoryResult(Success: true, orderPayload: new OrderPayload("Laptop", 1500.0, 5));

        _context.CallActivityAsync<InventoryResult>(
                nameof(ReserveInventoryActivity),
                Arg.Any<object?>(),
                Arg.Any<WorkflowTaskOptions?>())
            .Returns(inventoryResult);

        // Timer completes immediately (simulates timeout)
        _context.CreateTimer(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Approval event never arrives
        var neverCompletingApproval = new TaskCompletionSource<ApprovalDecision>().Task;
        _context.WaitForExternalEventAsync<ApprovalDecision>(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(neverCompletingApproval);

        // Act
        var result = await _workflow.RunAsync(_context, order);

        // Assert
        result.Processed.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_Should_CallHandleApprovalTimeoutActivity_When_ApprovalTimesOut()
    {
        // Arrange
        var order = new OrderPayload("Laptop", TotalCost: 1500.0, Quantity: 1);
        var inventoryResult = new InventoryResult(Success: true, orderPayload: new OrderPayload("Laptop", 1500.0, 5));

        _context.CallActivityAsync<InventoryResult>(
                nameof(ReserveInventoryActivity),
                Arg.Any<object?>(),
                Arg.Any<WorkflowTaskOptions?>())
            .Returns(inventoryResult);

        _context.CreateTimer(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _context.WaitForExternalEventAsync<ApprovalDecision>(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<ApprovalDecision>().Task);

        // Act
        await _workflow.RunAsync(_context, order);

        // Assert — timeout handler activity must be called
        await _context.Received(1).CallActivityAsync(
            nameof(HandleApprovalTimeoutActivity),
            Arg.Any<object?>(),
            Arg.Any<WorkflowTaskOptions?>());
    }

    #endregion

    #region UpdateInventoryActivity failure

    [Fact]
    public async Task RunAsync_Should_ReturnProcessedFalse_When_UpdateInventoryThrows()
    {
        // Arrange — inventory reserved, payment processed, but UpdateInventory fails
        var order = new OrderPayload("Widget", TotalCost: 100.0, Quantity: 1);
        var inventoryResult = new InventoryResult(Success: true, orderPayload: new OrderPayload("Widget", 100.0, 10));

        _context.CallActivityAsync<InventoryResult>(
                nameof(ReserveInventoryActivity),
                Arg.Any<object?>(),
                Arg.Any<WorkflowTaskOptions?>())
            .Returns(inventoryResult);

        // UpdateInventoryActivity throws TaskFailedException
        _context.CallActivityAsync(
                nameof(UpdateInventoryActivity),
                Arg.Any<object?>(),
                Arg.Any<WorkflowTaskOptions?>())
            .Returns(Task.FromException(new TaskFailedException("Inventory update failed")));

        // Act
        var result = await _workflow.RunAsync(_context, order);

        // Assert
        result.Processed.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsync_Should_SendRefundNotification_When_UpdateInventoryThrows()
    {
        // Arrange
        var order = new OrderPayload("Widget", TotalCost: 100.0, Quantity: 1);
        var inventoryResult = new InventoryResult(Success: true, orderPayload: new OrderPayload("Widget", 100.0, 10));

        _context.CallActivityAsync<InventoryResult>(
                nameof(ReserveInventoryActivity),
                Arg.Any<object?>(),
                Arg.Any<WorkflowTaskOptions?>())
            .Returns(inventoryResult);

        _context.CallActivityAsync(
                nameof(UpdateInventoryActivity),
                Arg.Any<object?>(),
                Arg.Any<WorkflowTaskOptions?>())
            .Returns(Task.FromException(new TaskFailedException("Inventory update failed")));

        // Capture notification messages
        var notificationMessages = new List<string>();
        _context.CallActivityAsync(
                nameof(NotifyActivity),
                Arg.Do<object?>(payload =>
                {
                    if (payload is Notification n) notificationMessages.Add(n.Message);
                }),
                Arg.Any<WorkflowTaskOptions?>())
            .Returns(Task.CompletedTask);

        // Act
        await _workflow.RunAsync(_context, order);

        // Assert — at least one notification mentions "refund"
        notificationMessages.Should().Contain(msg =>
            msg.Contains("refund", StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
