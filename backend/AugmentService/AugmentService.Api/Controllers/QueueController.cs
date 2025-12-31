using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using AugmentService.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;

namespace AugmentService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueueController : ControllerBase
    {
        private readonly ServiceBusAdministrationClient _serviceBusAdministrationClient;

        public QueueController(ServiceBusAdministrationClient serviceBusAdministrationClient)
        {
            _serviceBusAdministrationClient = serviceBusAdministrationClient;
        }

        [HttpGet]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<QueueStatus> Get()
        {
            //var connectionString = Configuration.GetValue<string>("KEDA_SERVICEBUS_QUEUE_CONNECTIONSTRING");

            // Check current queue length
            //var client = new ManagementClient(new ServiceBusConnectionStringBuilder(connectionString));
            //var queueInfo = await client.GetQueueRuntimeInfoAsync("orders");
            var queueInfo = await _serviceBusAdministrationClient.GetQueueRuntimePropertiesAsync("orders");

            return new QueueStatus
            {
                MessageCount = 10 //queueInfo...MessageCount
            }; 
        }
    }
}

