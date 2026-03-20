using Azure.Messaging.ServiceBus;
using Dapr.Client;
using AugmentService.Api.Models;
using AugmentService.Api.Workflows;
using AugmentService.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AugmentService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ServiceBusSender _serviceBusSender;
        private readonly IOrderWorkflowClient _workflowClient;
        private readonly DaprClient _daprClient;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            ServiceBusClient serviceBusClient,
            IOrderWorkflowClient workflowClient,
            DaprClient daprClient,
            ILogger<OrdersController> logger)
        {
            _serviceBusSender = serviceBusClient.CreateSender("orders");
            _workflowClient = workflowClient;
            _daprClient = daprClient;
            _logger = logger;
        }
        
        [HttpPost(Name = "Order_Create")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody, Required] Order order)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            // Map to workflow input type — explicit mapping avoids implicit JSON round-trip
            // and ensures TotalCost int→double widening is intentional.
            var orderPayload = new OrderPayload(order.Name, order.TotalCost, order.Quantity);

            _logger.LogInformation(
                "Starting order workflow: Name={Name}, Quantity={Quantity}, TotalCost={TotalCost}",
                orderPayload.Name, orderPayload.Quantity, orderPayload.TotalCost);
            
            var instanceId = await _workflowClient.ScheduleNewWorkflowAsync(
                name: nameof(OrderProcessingWorkflow),
                input: orderPayload);

            var response = new
            {
                InstanceId = instanceId
            };
            return Accepted(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var runtimeStatus = await _workflowClient.GetWorkflowStatusAsync(id);
            var response = new
            {
                WorkflowInstanceId = id,
                WorkflowStatus = runtimeStatus
            };

            return Ok(response);
        }

        
    }
}

