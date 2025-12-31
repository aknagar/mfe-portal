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

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="queueClient">Client to send messages to queue with</param>
        public OrdersController(ServiceBusClient serviceBusClient, DaprWorkflowClient daprWorkflowClient)
        {
            // Guard.NotNull(queueClient, nameof(queueClient));

            _serviceBusSender = serviceBusClient.CreateSender("orders");
            _daprWorkflowClient = daprWorkflowClient;
        }

        /// <summary>
        ///     Create Order
        /// </summary>
        [HttpPost(Name = "Order_Create")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody, Required] Order order)
        {
            var rawOrder = JsonConvert.SerializeObject(order);

            /*
            var orderMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(rawOrder));
            await _serviceBusSender.SendMessageAsync(orderMessage);
            */

            
            // Start the workflow
            Console.WriteLine("Starting workflow: Name={0}, Quantity={1}, TotalCost={2}", order.Name, order.Quantity, order.TotalCost);

            var instanceId = await _daprWorkflowClient.ScheduleNewWorkflowAsync(
                name: nameof(OrderProcessingWorkflow),
                input: order);

            /*
            // Wait for the workflow to start and confirm the input
            WorkflowState state = await _daprWorkflowClient.WaitForWorkflowStartAsync(
                instanceId: instanceId);

            Console.WriteLine("Your workflow has started. Here is the status of the workflow: {0}", Enum.GetName(typeof(WorkflowRuntimeStatus), state.RuntimeStatus));
            */
          
            var response = new
            {
                InstanceId = instanceId
            };
            return Accepted(response);
        }

        
        [HttpGet]
        public Task<string> Get()
        {
            return Task.FromResult<string>("Hello");
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

