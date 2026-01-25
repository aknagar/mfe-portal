using Dapr.Client;
using Dapr.Workflow;
using AugmentService.Api.Models;
using AugmentService.Core.Entities;

namespace AugmentService.Api.Activities
{
    class HandleApprovalTimeoutActivity : WorkflowActivity<ApprovalTimeoutPayload, ApprovalResult>
    {
        private readonly ILogger _logger;
        private readonly DaprClient _daprClient;
        private const string StateStoreName = "statestore";
        private const string ApprovalKeyPrefix = "approval_";

        public HandleApprovalTimeoutActivity(ILoggerFactory loggerFactory, DaprClient daprClient)
        {
            _logger = loggerFactory.CreateLogger<HandleApprovalTimeoutActivity>();
            _daprClient = daprClient;
        }

        public override async Task<ApprovalResult> RunAsync(WorkflowActivityContext context, ApprovalTimeoutPayload payload)
        {
            var stateKey = $"{ApprovalKeyPrefix}{payload.OrderId}";
            
            // Get current approval state
            var approval = await _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, stateKey);
            
            if (approval != null && approval.Status == ApprovalStatus.Pending)
            {
                // Update status to TimedOut
                approval.Status = ApprovalStatus.TimedOut;
                approval.ProcessedAt = DateTime.UtcNow;
                approval.Comments = "Approval request timed out after 24 hours";
                
                await _daprClient.SaveStateAsync(StateStoreName, stateKey, approval);
                
                _logger.LogWarning(
                    "Approval for order {OrderId} has timed out. Releasing reserved inventory.",
                    payload.OrderId);

                return new ApprovalResult(Success: false, Message: "Approval timed out");
            }

            return new ApprovalResult(Success: false, Message: "Approval already processed or not found");
        }
    }
}
