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

public class UpdateInventoryActivityTests
{
    private readonly DaprClient _daprClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly UpdateInventoryActivity _activity;

    public UpdateInventoryActivityTests()
    {
        _daprClient = Substitute.For<DaprClient>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _activity = new UpdateInventoryActivity(_loggerFactory, _daprClient);
    }

    private static WorkflowActivityContext MakeContext() =>
        Substitute.For<WorkflowActivityContext>();

    [Fact]
    public async Task RunAsync_Should_ThrowInvalidOperationException_When_ItemNotFoundInStateStore()
    {
        // Arrange
        var req = new PaymentRequest("req-001", "Widget", 1, 10.0);
        _daprClient
            .GetStateAndETagAsync<OrderPayload>("statestore", "Widget",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
#pragma warning disable CS8620
            .Returns(Task.FromResult<(OrderPayload?, string)>((null, "")));
#pragma warning restore CS8620

        // Act
        Func<Task> act = async () => await _activity.RunAsync(MakeContext(), req);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RunAsync_Should_ThrowInvalidOperationException_When_InsufficientInventory()
    {
        // Arrange — 2 in stock, trying to purchase 5
        var req = new PaymentRequest("req-002", "Widget", 5, 50.0);
        var inventoryItem = new OrderPayload("Widget", 10.0, Quantity: 2);
        _daprClient
            .GetStateAndETagAsync<OrderPayload>("statestore", "Widget",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns((inventoryItem, "etag-001"));

        // Act
        Func<Task> act = async () => await _activity.RunAsync(MakeContext(), req);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RunAsync_Should_ReturnNull_When_SufficientInventory()
    {
        // Arrange — 10 in stock, purchasing 3
        var req = new PaymentRequest("req-003", "Gadget", 3, 30.0);
        var inventoryItem = new OrderPayload("Gadget", 10.0, Quantity: 10);
        _daprClient
            .GetStateAndETagAsync<OrderPayload>("statestore", "Gadget",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns((inventoryItem, "etag-002"));

        // Act
        var result = await _activity.RunAsync(MakeContext(), req);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_Should_SaveUpdatedQuantity_When_SufficientInventory()
    {
        // Arrange — 10 in stock, purchasing 3 → should save 7
        var req = new PaymentRequest("req-004", "Widget", 3, 10.0);
        var inventoryItem = new OrderPayload("Widget", 10.0, Quantity: 10);
        _daprClient
            .GetStateAndETagAsync<OrderPayload>("statestore", "Widget",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns((inventoryItem, "etag-003"));

        // Act
        await _activity.RunAsync(MakeContext(), req);

        // Assert — saved with quantity = 10 - 3 = 7
        await _daprClient.Received(1).SaveStateAsync<OrderPayload>(
            "statestore",
            "Widget",
            Arg.Is<OrderPayload>(p => p.Quantity == 7),
            Arg.Any<StateOptions?>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_Should_ReturnNull_When_StockExactlyMatchesPurchaseAmount()
    {
        // Arrange — exactly 5 in stock, purchasing 5
        var req = new PaymentRequest("req-005", "Gizmo", 5, 25.0);
        var inventoryItem = new OrderPayload("Gizmo", 5.0, Quantity: 5);
        _daprClient
            .GetStateAndETagAsync<OrderPayload>("statestore", "Gizmo",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns((inventoryItem, "etag-004"));

        // Act
        var result = await _activity.RunAsync(MakeContext(), req);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_Should_PropagateException_When_DaprClientThrows()
    {
        // Arrange
        var req = new PaymentRequest("req-006", "Widget", 1, 10.0);
        _daprClient
            .GetStateAndETagAsync<OrderPayload>("statestore", "Widget",
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("State store unavailable"));

        // Act
        Func<Task> act = async () => await _activity.RunAsync(MakeContext(), req);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_Should_CreateLogger_Via_LoggerFactory()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var daprClient = Substitute.For<DaprClient>();

        _ = new UpdateInventoryActivity(loggerFactory, daprClient);

        loggerFactory.Received(1).CreateLogger(Arg.Any<string>());
    }
}
