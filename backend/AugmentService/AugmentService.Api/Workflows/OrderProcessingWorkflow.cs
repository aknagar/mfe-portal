using Dapr.Workflow;
using DurableTask.Core.Exceptions;
using AugmentService.Api.Activities;
using AugmentService.Api.Models;
using AugmentService.Api.Controllers;

namespace AugmentService.Api.Workflows
{
    class OrderProcessingWorkflow : Workflow<OrderPayload, OrderResult>
    {
        // Approval threshold - orders above this amount require approval
        private const double ApprovalThreshold = 1000.0;
        // Approval timeout duration
        private static readonly TimeSpan ApprovalTimeout = TimeSpan.FromHours(24);
        // Event name for approval decisions
        private const string ApprovalEventName = "ApprovalReceived";
        
        public override async Task<OrderResult> RunAsync(WorkflowContext context, OrderPayload order)
        {
            string orderId = context.InstanceId;

            // Notify the user that an order has come through
            await context.CallActivityAsync(
                nameof(NotifyActivity),
                new Notification($"Received order {orderId} for {order.Quantity} {order.Name} at ${order.TotalCost}"));

            string requestId = context.InstanceId;

            // Determine if there is enough of the item available for purchase by checking the inventory
            InventoryResult result = await context.CallActivityAsync<InventoryResult>(
                nameof(ReserveInventoryActivity),
                new InventoryRequest(RequestId: orderId, order.Name, order.Quantity));

            // If there is insufficient inventory, fail and let the user know 
            if (!result.Success)
            {
                // End the workflow here since we don't have sufficient inventory
                await context.CallActivityAsync(
                    nameof(NotifyActivity),
                    new Notification($"Insufficient inventory for {order.Name}"));
                return new OrderResult(Processed: false);
            }

            // Check if approval is required (orders above threshold)
            if (order.TotalCost >= ApprovalThreshold)
            {
                // Request approval
                await context.CallActivityAsync(
                    nameof(RequestApprovalActivity),
                    new ApprovalPayload(orderId, order.Name, order.TotalCost, order.Quantity));

                await context.CallActivityAsync(
                    nameof(NotifyActivity),
                    new Notification($"Order {orderId} requires approval (${order.TotalCost} >= ${ApprovalThreshold}). Waiting for manager approval..."));

                // Wait for approval with 24-hour timeout
                using var timeoutCts = new CancellationTokenSource();
                var approvalTask = context.WaitForExternalEventAsync<ApprovalDecision>(ApprovalEventName);
                var timeoutTask = context.CreateTimer(ApprovalTimeout);

                var completedTask = await Task.WhenAny(approvalTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // Approval timed out
                    await context.CallActivityAsync(
                        nameof(HandleApprovalTimeoutActivity),
                        new ApprovalTimeoutPayload(orderId));

                    await context.CallActivityAsync(
                        nameof(NotifyActivity),
                        new Notification($"Order {orderId} approval timed out after 24 hours. Order cancelled and inventory released."));

                    return new OrderResult(Processed: false);
                }

                // Approval response received
                var approvalDecision = await approvalTask;

                if (!approvalDecision.IsApproved)
                {
                    await context.CallActivityAsync(
                        nameof(NotifyActivity),
                        new Notification($"Order {orderId} was rejected by {approvalDecision.ApprovedBy ?? "manager"}. Reason: {approvalDecision.Comments ?? "No reason provided"}"));

                    return new OrderResult(Processed: false);
                }

                await context.CallActivityAsync(
                    nameof(NotifyActivity),
                    new Notification($"Order {orderId} was approved by {approvalDecision.ApprovedBy ?? "manager"}. Proceeding with payment..."));
            }

            // There is enough inventory available so the user can purchase the item(s). Process their payment
            await context.CallActivityAsync(
                nameof(ProcessPaymentActivity),
                new PaymentRequest(RequestId: orderId, order.Name, order.Quantity, order.TotalCost));

            try
            {
                // There is enough inventory available so the user can purchase the item(s). Process their payment
                await context.CallActivityAsync(
                    nameof(UpdateInventoryActivity),
                    new PaymentRequest(RequestId: orderId, order.Name, order.Quantity, order.TotalCost));
            }
            catch (TaskFailedException)
            {
                // Let them know their payment was processed
                await context.CallActivityAsync(
                    nameof(NotifyActivity),
                    new Notification($"Order {orderId} Failed! You are now getting a refund"));
                return new OrderResult(Processed: false);
            }

            // Let them know their payment was processed
            await context.CallActivityAsync(
                nameof(NotifyActivity),
                new Notification($"Order {orderId} has completed!"));

            // End the workflow with a success result
            return new OrderResult(Processed: true);
        }
       
    }
     
}

