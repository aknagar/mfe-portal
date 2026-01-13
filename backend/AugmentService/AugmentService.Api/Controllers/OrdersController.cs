using Azure.Messaging.ServiceBus;
using Dapr.Client;
using Dapr.Workflow;
using AugmentService.Api.Workflows;
using AugmentService.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AugmentService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ServiceBusSender _serviceBusSender;
        private readonly DaprWorkflowClient _daprWorkflowClient;

        public OrdersController(ServiceBusClient serviceBusClient, DaprWorkflowClient daprWorkflowClient)
        {
            // Guard.NotNull(queueClient, nameof(queueClient));
            _serviceBusSender = serviceBusClient.CreateSender("orders");
            _daprWorkflowClient = daprWorkflowClient;
        }
        
        [HttpPost(Name = "Order_Create")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody, Required] Order order)
        {
            var rawOrder = JsonConvert.SerializeObject(order);
            
            // Start the workflow
            Console.WriteLine("Starting workflow: Name={0}, Quantity={1}, TotalCost={2}", order.Name, order.Quantity, order.TotalCost);

            var instanceId = await _daprWorkflowClient.ScheduleNewWorkflowAsync(
                name: nameof(OrderProcessingWorkflow),
                input: order);

            var response = new
            {
                InstanceId = instanceId
            };
            return Accepted(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var workflowState = await _daprWorkflowClient.GetWorkflowStateAsync(id);
            var response = new
            {
                WorkflowInstanceId = id,
                WorkflowStatus = workflowState.RuntimeStatus
            };

            return Ok(response);
        }

        
    }
}

