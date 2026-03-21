using Dapr.Client;
using Dapr.Workflow;
using AugmentService.Api.Models;
namespace AugmentService.Api.Activities
{
    class ReserveInventoryActivity : WorkflowActivity<InventoryRequest, InventoryResult>
    {
        readonly ILogger logger;
        readonly DaprClient client;
        static readonly string storeName = "statestore";

        public ReserveInventoryActivity(ILoggerFactory loggerFactory, DaprClient client)
        {
            this.logger = loggerFactory.CreateLogger<ReserveInventoryActivity>();
            this.client = client;
        }

        public override async Task<InventoryResult> RunAsync(WorkflowActivityContext context, InventoryRequest req)
        {
            // Guard: ItemName is the Dapr state store key — null/empty would throw inside the SDK.
            // This is a programming error (invalid workflow input), not an expected business case.
            if (string.IsNullOrEmpty(req.ItemName))
            {
                this.logger.LogError(
                    "ReserveInventory for order {RequestId}: ItemName is null or empty. " +
                    "The Order.Name must be validated before scheduling the workflow.",
                    req.RequestId);
                return new InventoryResult(false, null);
            }

            this.logger.LogInformation(
                "Reserving inventory for order {RequestId}: {Quantity} x '{ItemName}'",
                req.RequestId,
                req.Quantity,
                req.ItemName);

            OrderPayload? orderResponse;
            (orderResponse, _) = await client.GetStateAndETagAsync<OrderPayload>(storeName, req.ItemName);

            if (orderResponse == null)
            {
                this.logger.LogWarning(
                    "ReserveInventory for order {RequestId}: item '{ItemName}' not found in inventory state store '{StoreName}'.",
                    req.RequestId,
                    req.ItemName,
                    storeName);
                return new InventoryResult(false, null);
            }

            this.logger.LogInformation(
                "Inventory check for order {RequestId}: {Available} units of '{ItemName}' available, {Requested} requested.",
                req.RequestId,
                orderResponse.Quantity,
                orderResponse.Name,
                req.Quantity);

            if (orderResponse.Quantity >= req.Quantity)
            {
                // Simulate slow processing
                await Task.Delay(TimeSpan.FromSeconds(2));

                return new InventoryResult(true, orderResponse);
            }

            this.logger.LogWarning(
                "ReserveInventory for order {RequestId}: insufficient stock for '{ItemName}' — {Available} available, {Requested} requested.",
                req.RequestId,
                req.ItemName,
                orderResponse.Quantity,
                req.Quantity);
            return new InventoryResult(false, orderResponse);
        }
    }
}

