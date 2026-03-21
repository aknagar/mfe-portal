using Dapr.Workflow;
using AugmentService.Api.Controllers;
using AugmentService.Api.Workflows;
using AugmentService.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AugmentService.Api.UnitTests.Controllers;

public class OrdersControllerTests
{
    private readonly IOrderWorkflowClient _workflowClient;
    private readonly ILogger<OrdersController> _logger;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _workflowClient = Substitute.For<IOrderWorkflowClient>();
        _logger = Substitute.For<ILogger<OrdersController>>();

        _controller = new OrdersController(
            _workflowClient,
            _logger);
    }

    #region Create — happy path

    [Fact]
    public async Task Create_Should_ReturnAccepted_When_OrderIsValid()
    {
        // Arrange
        var order = new Order { Name = "Widget", TotalCost = 50, Quantity = 2 };
        var instanceId = Guid.NewGuid().ToString();
        _workflowClient
            .ScheduleNewWorkflowAsync(
                name: Arg.Any<string>(),
                instanceId: Arg.Any<string?>(),
                input: Arg.Any<object?>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(instanceId);

        // Act
        var result = await _controller.Create(order);

        // Assert
        result.Should().BeOfType<AcceptedResult>();
    }

    [Fact]
    public async Task Create_Should_ScheduleWorkflow_When_OrderIsValid()
    {
        // Arrange
        var order = new Order { Name = "Gadget", TotalCost = 200, Quantity = 3 };
        _workflowClient
            .ScheduleNewWorkflowAsync(
                name: Arg.Any<string>(),
                instanceId: Arg.Any<string?>(),
                input: Arg.Any<object?>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid().ToString());

        // Act
        await _controller.Create(order);

        // Assert — workflow was scheduled exactly once
        await _workflowClient.Received(1).ScheduleNewWorkflowAsync(
            name: "OrderProcessingWorkflow",
            instanceId: Arg.Any<string?>(),
            input: Arg.Any<object?>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_Should_ReturnInstanceId_When_WorkflowScheduled()
    {
        // Arrange
        var order = new Order { Name = "Widget", TotalCost = 100, Quantity = 1 };
        var expectedInstanceId = "test-instance-123";
        _workflowClient
            .ScheduleNewWorkflowAsync(
                name: Arg.Any<string>(),
                instanceId: Arg.Any<string?>(),
                input: Arg.Any<object?>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(expectedInstanceId);

        // Act
        var result = await _controller.Create(order);

        // Assert — response body contains the instance ID
        var accepted = result.Should().BeOfType<AcceptedResult>().Subject;
        var body = accepted.Value!;
        body.Should().BeEquivalentTo(new { InstanceId = expectedInstanceId });
    }

    [Fact]
    public async Task Create_Should_PassCorrectOrderPayload_When_WorkflowScheduled()
    {
        // Arrange
        var order = new Order { Name = "SpecialItem", TotalCost = 500, Quantity = 4 };
        object? capturedInput = null;
        _workflowClient
            .ScheduleNewWorkflowAsync(
                name: Arg.Any<string>(),
                instanceId: Arg.Any<string?>(),
                input: Arg.Do<object?>(i => capturedInput = i),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid().ToString());

        // Act
        await _controller.Create(order);

        // Assert — the workflow receives an OrderPayload (not the raw Order entity)
        capturedInput.Should().NotBeNull();
        capturedInput!.GetType().Name.Should().Be("OrderPayload");

        // Verify property values are passed through correctly
        var nameProperty = capturedInput.GetType().GetProperty("Name")!.GetValue(capturedInput);
        var totalCostProperty = capturedInput.GetType().GetProperty("TotalCost")!.GetValue(capturedInput);
        var quantityProperty = capturedInput.GetType().GetProperty("Quantity")!.GetValue(capturedInput);

        nameProperty.Should().Be("SpecialItem");
        totalCostProperty.Should().Be(500.0);  // int→double widening
        quantityProperty.Should().Be(4);
    }

    #endregion

    #region Create — invalid model state

    [Fact]
    public async Task Create_Should_ReturnValidationProblem_When_ModelStateIsInvalid()
    {
        // Arrange
        var order = new Order { Name = "", TotalCost = 100, Quantity = 1 };
        _controller.ModelState.AddModelError(nameof(Order.Name), "Name must not be empty.");

        // Act
        var result = await _controller.Create(order);

        // Assert — ValidationProblem() wraps errors in a ValidationProblemDetails.
        // Status is only set by the response pipeline, not at construction time;
        // asserting on the Errors dictionary is the reliable approach.
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        var problemDetails = objectResult.Value.Should().BeOfType<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>().Subject;
        problemDetails.Errors.Should().ContainKey(nameof(Order.Name));
    }

    [Fact]
    public async Task Create_Should_NotScheduleWorkflow_When_ModelStateIsInvalid()
    {
        // Arrange
        var order = new Order { Name = "", TotalCost = 100, Quantity = 1 };
        _controller.ModelState.AddModelError(nameof(Order.Name), "Name must not be empty.");

        // Act
        await _controller.Create(order);

        // Assert — workflow must NOT be scheduled
        await _workflowClient.DidNotReceive().ScheduleNewWorkflowAsync(
            name: Arg.Any<string>(),
            instanceId: Arg.Any<string?>(),
            input: Arg.Any<object?>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    #endregion

    #region Create — downstream failure

    [Fact]
    public async Task Create_Should_PropagateException_When_WorkflowClientThrows()
    {
        // Arrange
        var order = new Order { Name = "Widget", TotalCost = 100, Quantity = 1 };
        _workflowClient
            .ScheduleNewWorkflowAsync(
                name: Arg.Any<string>(),
                instanceId: Arg.Any<string?>(),
                input: Arg.Any<object?>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Dapr sidecar unavailable"));

        // Act
        Func<Task> act = async () => await _controller.Create(order);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Dapr sidecar unavailable*");
    }

    #endregion

    #region Get

    [Fact]
    public async Task Get_Should_ReturnOk_When_WorkflowStatusRetrieved()
    {
        // Arrange
        var instanceId = "order-abc-123";
        _workflowClient
            .GetWorkflowStatusAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns(WorkflowRuntimeStatus.Running);

        // Act
        var result = await _controller.Get(instanceId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Get_Should_ReturnCorrectInstanceId_When_WorkflowStatusRetrieved()
    {
        // Arrange
        var instanceId = "order-xyz-789";
        _workflowClient
            .GetWorkflowStatusAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns(WorkflowRuntimeStatus.Completed);

        // Act
        var result = await _controller.Get(instanceId);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value!;
        var workflowInstanceId = body.GetType().GetProperty("WorkflowInstanceId")!.GetValue(body);
        workflowInstanceId.Should().Be(instanceId);
    }

    [Fact]
    public async Task Get_Should_CallGetWorkflowStatus_WithCorrectInstanceId()
    {
        // Arrange
        var instanceId = "my-order-id";
        _workflowClient
            .GetWorkflowStatusAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns(WorkflowRuntimeStatus.Running);

        // Act
        await _controller.Get(instanceId);

        // Assert
        await _workflowClient.Received(1).GetWorkflowStatusAsync(
            instanceId,
            Arg.Any<CancellationToken>());
    }

    #endregion
}
