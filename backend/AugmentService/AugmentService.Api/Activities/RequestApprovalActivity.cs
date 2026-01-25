using Dapr.Client;
using Dapr.Workflow;
using AugmentService.Api.Models;
using AugmentService.Core.Entities;

namespace AugmentService.Api.Activities
{
    class RequestApprovalActivity : WorkflowActivity<ApprovalPayload, ApprovalResult>
    {
        private readonly ILogger _logger;
        private readonly DaprClient _daprClient;
        private const string StateStoreName = "statestore";
        private const string ApprovalKeyPrefix = "approval_";

        public RequestApprovalActivity(ILoggerFactory loggerFactory, DaprClient daprClient)
        {
            _logger = loggerFactory.CreateLogger<RequestApprovalActivity>();
            _daprClient = daprClient;
        }

        public override async Task<ApprovalResult> RunAsync(WorkflowActivityContext context, ApprovalPayload payload)
        {
            var approvalRequest = new ApprovalRequest
            {
                OrderId = payload.OrderId,
                OrderName = payload.OrderName,
                TotalCost = payload.TotalCost,
                Quantity = payload.Quantity,
                Status = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            // Store approval request in Dapr state store
            var stateKey = $"{ApprovalKeyPrefix}{payload.OrderId}";
            await _daprClient.SaveStateAsync(StateStoreName, stateKey, approvalRequest);

            _logger.LogInformation(
                "Approval requested for order {OrderId}: {Quantity}x {OrderName} at ${TotalCost}. Expires at {ExpiresAt}",
                payload.OrderId, payload.Quantity, payload.OrderName, payload.TotalCost, approvalRequest.ExpiresAt);

            return new ApprovalResult(Success: true, Message: "Approval request created");
        }
    }
}
