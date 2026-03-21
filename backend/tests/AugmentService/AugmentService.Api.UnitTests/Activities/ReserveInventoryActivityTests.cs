using Dapr.Client;
using Dapr.Workflow;
using AugmentService.Api.Activities;
using AugmentService.Api.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AugmentService.Api.UnitTests.Activities;

public class ReserveInventoryActivityTests
{
    private readonly DaprClient _daprClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ReserveInventoryActivity _activity;

    public ReserveInventoryActivityTests()
    {
        _daprClient = Substitute.For<DaprClient>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _activity = new ReserveInventoryActivity(_loggerFactory, _daprClient);
    }

    private static WorkflowActivityContext MakeContext() =>
        Substitute.For<WorkflowActivityContext>();

    #region Guard: null / empty ItemName

    [Fact]
    public async Task RunAsync_Should_ReturnFailure_When_ItemNameIsNull()
    {
        // Arrange
        var req = new InventoryRequest("req-001", null!, 1);

        // Act
        var result = await _activity.RunAsync(MakeContext(), req);

        // Assert
        result.Success.Should().BeFalse();
        result.orderPayload.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_Should_ReturnFailure_When_ItemNameIsEmpty()
    {
        // Arrange
        var req = new InventoryRequest("req-002", "", 1);

        // Act
        var result = await _activity.RunAsync(MakeContext(), req);

        // Assert
        result.Success.Should().BeFalse();
        result.orderPayload.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_Should_NotCallDaprClient_When_ItemNameIsNullOrEmpty()
    {
        // Arrange
        var req = new InventoryRequest("req-003", null!, 2);

        // Act
        await _activity.RunAsync(MakeContext(), req);

        // Assert — Dapr state store must NOT be called (guard prevents the SDK throw)
        await _daprClient.DidNotReceive().GetStateAndETagAsync<OrderPayload>(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<ConsistencyMode?>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Item not found in state store

    [Fact]
    public async Task RunAsync_Should_ReturnFailure_When_ItemNotFoundInStateStore()
    {
        // Arrange
        var req = new InventoryRequest("req-004", "Widget", 1);
        _daprClient
            .GetStateAndETagAsync<OrderPayload>("statestore", "Widget",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns((null, ""));

        // Act
        var result = await _activity.RunAsync(MakeContext(), req);

        // Assert
        result.Success.Should().BeFalse();
        result.orderPayload.Should().BeNull();
    }

    #endregion

    #region Insufficient stock

    [Fact]
    public async Task RunAsync_Should_ReturnFailure_When_InsufficientStock()
    {
        // Arrange — only 2 in stock, requesting 5
        var req = new InventoryRequest("req-005", "Widget", 5);
        var inventoryItem = new OrderPayload("Widget", 10.0, Quantity: 2);
        _daprClient
            .GetStateAndETagAsync<OrderPayload>("statestore", "Widget",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns((inventoryItem, "etag-abc"));

        // Act
        var result = await _activity.RunAsync(MakeContext(), req);

        // Assert
        result.Success.Should().BeFalse();
        result.orderPayload.Should().BeEquivalentTo(inventoryItem);
    }

    [Fact]
    public async Task RunAsync_Should_ReturnFailure_When_StockExactlyBelowRequestedQuantity()
    {
        // Arrange — 4 in stock, requesting 5 (boundary: 4 < 5)
        var req = new InventoryRequest("req-006", "Gadget", 5);
        var inventoryItem = new OrderPayload("Gadget", 20.0, Quantity: 4);
        _daprClient
            .GetStateAndETagAsync<OrderPayload>("statestore", "Gadget",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns((inventoryItem, "etag-def"));

        // Act
        var result = await _activity.RunAsync(MakeContext(), req);

        // Assert
        result.Success.Should().BeFalse();
    }

    #endregion

    #region Sufficient stock

    [Fact]
    public async Task RunAsync_Should_ReturnSuccess_When_SufficientStockAvailable()
    {
        // Arrange — 10 in stock, requesting 3
        var req = new InventoryRequest("req-007", "Widget", 3);
        var inventoryItem = new OrderPayload("Widget", 15.0, Quantity: 10);
        _daprClient
            .GetStateAndETagAsync<OrderPayload>("statestore", "Widget",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns((inventoryItem, "etag-ghi"));

        // Act
        var result = await _activity.RunAsync(MakeContext(), req);

        // Assert
        result.Success.Should().BeTrue();
        result.orderPayload.Should().BeEquivalentTo(inventoryItem);
    }

    [Fact]
    public async Task RunAsync_Should_ReturnSuccess_When_StockExactlyMatchesRequestedQuantity()
    {
        // Arrange — exactly 5 in stock, requesting 5 (boundary: 5 >= 5)
        var req = new InventoryRequest("req-008", "Gizmo", 5);
        var inventoryItem = new OrderPayload("Gizmo", 30.0, Quantity: 5);
        _daprClient
            .GetStateAndETagAsync<OrderPayload>("statestore", "Gizmo",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns((inventoryItem, "etag-jkl"));

        // Act
        var result = await _activity.RunAsync(MakeContext(), req);

        // Assert
        result.Success.Should().BeTrue();
        result.orderPayload.Should().BeEquivalentTo(inventoryItem);
    }

    [Fact]
    public async Task RunAsync_Should_ReturnCorrectPayload_When_SuccessfulReservation()
    {
        // Arrange
        var req = new InventoryRequest("req-009", "SuperWidget", 1);
        var inventoryItem = new OrderPayload("SuperWidget", 99.99, Quantity: 50);
        _daprClient
            .GetStateAndETagAsync<OrderPayload>("statestore", "SuperWidget",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns((inventoryItem, "etag-mno"));

        // Act
        var result = await _activity.RunAsync(MakeContext(), req);

        // Assert
        result.Success.Should().BeTrue();
        result.orderPayload!.Name.Should().Be("SuperWidget");
        result.orderPayload.TotalCost.Should().Be(99.99);
        result.orderPayload.Quantity.Should().Be(50);
    }

    #endregion

    #region Dapr client errors

    [Fact]
    public async Task RunAsync_Should_PropagateException_When_DaprClientThrows()
    {
        // Arrange
        var req = new InventoryRequest("req-010", "Widget", 1);
        _daprClient
            .GetStateAndETagAsync<OrderPayload>("statestore", "Widget",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("State store unavailable"));

        // Act
        Func<Task> act = async () => await _activity.RunAsync(MakeContext(), req);

        // Assert — Dapr infrastructure errors propagate (not swallowed)
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
