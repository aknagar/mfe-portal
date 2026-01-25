using Dapr.Client;
using Dapr.Workflow;
using AugmentService.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AugmentService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApprovalsController : ControllerBase
    {
        private readonly DaprWorkflowClient _daprWorkflowClient;
        private readonly DaprClient _daprClient;
        private readonly ILogger<ApprovalsController> _logger;
        private const string StateStoreName = "statestore";
        private const string ApprovalKeyPrefix = "approval_";
        private const string ApprovalEventName = "ApprovalReceived";

        public ApprovalsController(
            DaprWorkflowClient daprWorkflowClient, 
            DaprClient daprClient,
            ILogger<ApprovalsController> logger)
        {
            _daprWorkflowClient = daprWorkflowClient;
            _daprClient = daprClient;
            _logger = logger;
        }

        /// <summary>
        /// Get all pending approval requests
        /// </summary>
        [HttpGet(Name = "Approvals_GetPending")]
        [ProducesResponseType(typeof(IEnumerable<ApprovalRequest>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingApprovals()
        {
            // Query all approval keys from state store
            // Note: In production, consider using a separate index or query API
            var query = """
            {
                "filter": {
                    "EQ": { "status": 0 }
                }
            }
            """;

            try
            {
                var result = await _daprClient.QueryStateAsync<ApprovalRequest>(
                    StateStoreName, 
                    query);

                var pendingApprovals = result.Results
                    .Select(r => r.Data)
                    .OfType<ApprovalRequest>()
                    .Where(a => a.Status == ApprovalStatus.Pending)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToList();

                return Ok(pendingApprovals);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Query state not supported, returning empty list");
                // Fallback: return empty list if query is not supported by state store
                return Ok(Array.Empty<ApprovalRequest>());
            }
        }

        /// <summary>
        /// Get a specific approval request by order ID
        /// </summary>
        [HttpGet("{orderId}", Name = "Approvals_GetById")]
        [ProducesResponseType(typeof(ApprovalRequest), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetApproval(string orderId)
        {
            var stateKey = $"{ApprovalKeyPrefix}{orderId}";
            var approval = await _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, stateKey);

            if (approval == null)
            {
                return NotFound(new { Message = $"Approval request for order {orderId} not found" });
            }

            return Ok(approval);
        }

        /// <summary>
        /// Approve an order
        /// </summary>
        [HttpPost("{orderId}/approve", Name = "Approvals_Approve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Approve(string orderId, [FromBody] ApprovalDecisionRequest? request)
        {
            return await ProcessApprovalDecision(orderId, isApproved: true, request?.ApprovedBy, request?.Comments);
        }

        /// <summary>
        /// Reject an order
        /// </summary>
        [HttpPost("{orderId}/reject", Name = "Approvals_Reject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Reject(string orderId, [FromBody] ApprovalDecisionRequest? request)
        {
            return await ProcessApprovalDecision(orderId, isApproved: false, request?.ApprovedBy, request?.Comments);
        }

        private async Task<IActionResult> ProcessApprovalDecision(
            string orderId, 
            bool isApproved, 
            string? processedBy, 
            string? comments)
        {
            var stateKey = $"{ApprovalKeyPrefix}{orderId}";
            var approval = await _daprClient.GetStateAsync<ApprovalRequest>(StateStoreName, stateKey);

            if (approval == null)
            {
                return NotFound(new { Message = $"Approval request for order {orderId} not found" });
            }

            if (approval.Status != ApprovalStatus.Pending)
            {
                return BadRequest(new { Message = $"Approval request is already {approval.Status}" });
            }

            if (DateTime.UtcNow > approval.ExpiresAt)
            {
                return BadRequest(new { Message = "Approval request has expired" });
            }

            // Update approval state
            approval.Status = isApproved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
            approval.ProcessedAt = DateTime.UtcNow;
            approval.ProcessedBy = processedBy ?? "Unknown";
            approval.Comments = comments;

            await _daprClient.SaveStateAsync(StateStoreName, stateKey, approval);

            // Raise event to resume the waiting workflow
            try
            {
                await _daprWorkflowClient.RaiseEventAsync(
                    instanceId: orderId,
                    eventName: ApprovalEventName,
                    eventPayload: new ApprovalDecision(isApproved, processedBy, comments));

                _logger.LogInformation(
                    "Approval decision for order {OrderId}: {Decision} by {ProcessedBy}",
                    orderId, isApproved ? "Approved" : "Rejected", processedBy);

                return Ok(new 
                { 
                    Message = $"Order {orderId} has been {(isApproved ? "approved" : "rejected")}",
                    Approval = approval
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to raise approval event for order {OrderId}", orderId);
                return StatusCode(500, new { Message = "Failed to process approval decision" });
            }
        }
    }

    public record ApprovalDecisionRequest(string? ApprovedBy, string? Comments);
    public record ApprovalDecision(bool IsApproved, string? ApprovedBy, string? Comments);
}
