using Dapr.Client;
using Dapr.Workflow;
using AugmentService.Api.Controllers;
using AugmentService.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AugmentService.Api.UnitTests.Controllers;

public class ApprovalsControllerTests
{
    private readonly DaprWorkflowClient _workflowClient;
    private readonly DaprClient _daprClient;
    private readonly ILogger<ApprovalsController> _logger;
    private readonly ApprovalsController _controller;
    private const string StateStoreName = "statestore";

    public ApprovalsControllerTests()
    {
        _workflowClient = Substitute.For<DaprWorkflowClient>();
        _daprClient = Substitute.For<DaprClient>();
        _logger = Substitute.For<ILogger<ApprovalsController>>();
        _controller = new ApprovalsController(_workflowClient, _daprClient, _logger);
    }

    private static ApprovalRequest MakePendingApproval(string orderId) => new()
    {
        OrderId = orderId, OrderName = "Item",
        Status = ApprovalStatus.Pending,
        ExpiresAt = DateTime.UtcNow.AddHours(24)
    };

    [Fact]
    public async Task GetPendingApprovals_Should_ReturnOk_When_QuerySucceeds()
    {
        var response = Substitute.For<StateQueryResponse<ApprovalRequest>>();
        response.Results.Returns(new List<StateQueryItem<ApprovalRequest>>());
        _daprClient.QueryStateAsync<ApprovalRequest>(StateStoreName, Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns(response);
        var result = await _controller.GetPendingApprovals();
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPendingApprovals_Should_ReturnEmptyList_When_QueryThrows()
    {
        _daprClient.QueryStateAsync<ApprovalRequest>(StateStoreName, Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Query not supported"));
        var result = await _controller.GetPendingApprovals();
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(Array.Empty<ApprovalRequest>());
    }

    [Fact]
    public async Task GetPendingApprovals_Should_FilterOnlyPendingItems()
    {
        var pending = new ApprovalRequest { OrderId = "ORD-1", OrderName = "A", Status = ApprovalStatus.Pending, ExpiresAt = DateTime.UtcNow.AddHours(24) };
        var approved = new ApprovalRequest { OrderId = "ORD-2", OrderName = "B", Status = ApprovalStatus.Approved, ExpiresAt = DateTime.UtcNow.AddHours(24) };
        var items = new List<StateQueryItem<ApprovalRequest>> { new("k1", pending, "e1", ""), new("k2", approved, "e2", "") };
        var response = Substitute.For<StateQueryResponse<ApprovalRequest>>();
        response.Results.Returns(items);
        _daprClient.QueryStateAsync<ApprovalRequest>(StateStoreName, Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns(response);
        var result = await _controller.GetPendingApprovals();
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<ApprovalRequest>>().Subject.ToList();
        list.Should().HaveCount(1);
        list[0].OrderId.Should().Be("ORD-1");
    }

    [Fact]
    public async Task GetApproval_Should_ReturnOk_When_Found()
    {
        var orderId = "ORD-100";
        _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, $"approval_{orderId}", Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns(MakePendingApproval(orderId));
        var result = await _controller.GetApproval(orderId);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetApproval_Should_ReturnNotFound_When_Missing()
    {
        var orderId = "ORD-MISS";
        _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, $"approval_{orderId}", Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns((ApprovalRequest?)null);
        var result = await _controller.GetApproval(orderId);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetApproval_Should_ReturnData_When_Found()
    {
        var orderId = "ORD-200";
        var approval = MakePendingApproval(orderId);
        _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, $"approval_{orderId}", Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns(approval);
        var result = await _controller.GetApproval(orderId);
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(approval);
    }

    [Fact]
    public async Task Approve_Should_ReturnNotFound_When_ApprovalMissing()
    {
        var orderId = "ORD-300";
        _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, $"approval_{orderId}", Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns((ApprovalRequest?)null);
        var result = await _controller.Approve(orderId, new ApprovalDecisionRequest("admin", null));
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Approve_Should_ReturnBadRequest_When_AlreadyProcessed()
    {
        var orderId = "ORD-301";
        var approval = new ApprovalRequest { OrderId = orderId, OrderName = "W", Status = ApprovalStatus.Approved, ExpiresAt = DateTime.UtcNow.AddHours(24) };
        _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, $"approval_{orderId}", Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns(approval);
        var result = await _controller.Approve(orderId, new ApprovalDecisionRequest("admin", null));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Approve_Should_ReturnBadRequest_When_Expired()
    {
        var orderId = "ORD-302";
        var approval = new ApprovalRequest { OrderId = orderId, OrderName = "W", Status = ApprovalStatus.Pending, ExpiresAt = DateTime.UtcNow.AddHours(-1) };
        _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, $"approval_{orderId}", Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns(approval);
        var result = await _controller.Approve(orderId, new ApprovalDecisionRequest("admin", null));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Approve_Should_ReturnOk_When_Succeeds()
    {
        var orderId = "ORD-303";
        _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, $"approval_{orderId}", Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns(MakePendingApproval(orderId));
        var result = await _controller.Approve(orderId, new ApprovalDecisionRequest("admin", "OK"));
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Approve_Should_RaiseWorkflowEventIsApprovedTrue()
    {
        var orderId = "ORD-304";
        _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, $"approval_{orderId}", Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns(MakePendingApproval(orderId));
        await _controller.Approve(orderId, new ApprovalDecisionRequest("admin", null));
        await _workflowClient.Received(1).RaiseEventAsync(orderId, "ApprovalReceived", Arg.Is<ApprovalDecision>(d => d.IsApproved), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_Should_Return500_When_WorkflowThrows()
    {
        var orderId = "ORD-305";
        _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, $"approval_{orderId}", Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns(MakePendingApproval(orderId));
        _workflowClient.RaiseEventAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("Dapr error"));
        var result = await _controller.Approve(orderId, new ApprovalDecisionRequest("admin", null));
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Approve_Should_UseUnknown_When_ApprovedByIsNull()
    {
        var orderId = "ORD-306";
        ApprovalRequest? saved = null;
        _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, $"approval_{orderId}", Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns(MakePendingApproval(orderId));
        await _daprClient.SaveStateAsync(StateStoreName, Arg.Any<string>(), Arg.Do<ApprovalRequest>(r => saved = r), Arg.Any<StateOptions?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>());
        await _controller.Approve(orderId, new ApprovalDecisionRequest(null, null));
        saved.Should().NotBeNull();
        saved!.ProcessedBy.Should().Be("Unknown");
    }

    [Fact]
    public async Task Reject_Should_ReturnNotFound_When_ApprovalMissing()
    {
        var orderId = "ORD-400";
        _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, $"approval_{orderId}", Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns((ApprovalRequest?)null);
        var result = await _controller.Reject(orderId, new ApprovalDecisionRequest("manager", null));
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Reject_Should_ReturnOk_When_Succeeds()
    {
        var orderId = "ORD-401";
        _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, $"approval_{orderId}", Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns(MakePendingApproval(orderId));
        var result = await _controller.Reject(orderId, new ApprovalDecisionRequest("manager", "Budget exceeded"));
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Reject_Should_RaiseWorkflowEventIsApprovedFalse()
    {
        var orderId = "ORD-402";
        _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, $"approval_{orderId}", Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns(MakePendingApproval(orderId));
        await _controller.Reject(orderId, new ApprovalDecisionRequest("manager", null));
        await _workflowClient.Received(1).RaiseEventAsync(orderId, "ApprovalReceived", Arg.Is<ApprovalDecision>(d => !d.IsApproved), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_Should_ReturnBadRequest_When_AlreadyRejected()
    {
        var orderId = "ORD-403";
        var approval = new ApprovalRequest { OrderId = orderId, OrderName = "W", Status = ApprovalStatus.Rejected, ExpiresAt = DateTime.UtcNow.AddHours(24) };
        _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, $"approval_{orderId}", Arg.Any<ConsistencyMode?>(), Arg.Any<IReadOnlyDictionary<string, string>?>(), Arg.Any<CancellationToken>()).Returns(approval);
        var result = await _controller.Reject(orderId, new ApprovalDecisionRequest("manager", null));
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
