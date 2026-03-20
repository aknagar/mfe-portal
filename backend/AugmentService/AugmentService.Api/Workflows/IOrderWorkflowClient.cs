using Dapr.Workflow;

namespace AugmentService.Api.Workflows;

/// <summary>
/// Abstraction over DaprWorkflowClient for the order workflow operations.
/// DaprWorkflowClient is a concrete non-virtual class, so this interface
/// allows the controller to be unit-tested without a live Dapr sidecar.
/// </summary>
public interface IOrderWorkflowClient
{
    Task<string> ScheduleNewWorkflowAsync(
        string name,
        string? instanceId = null,
        object? input = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the runtime status of the specified workflow instance.
    /// WorkflowState has an internal constructor so is not easily testable;
    /// returning only the status keeps the interface simple and testable.
    /// </summary>
    Task<WorkflowRuntimeStatus> GetWorkflowStatusAsync(
        string instanceId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Production implementation — thin wrapper over <see cref="DaprWorkflowClient"/>.
/// </summary>
internal sealed class DaprOrderWorkflowClient(DaprWorkflowClient inner) : IOrderWorkflowClient
{
    public Task<string> ScheduleNewWorkflowAsync(
        string name,
        string? instanceId = null,
        object? input = null,
        CancellationToken cancellationToken = default)
        => inner.ScheduleNewWorkflowAsync(name, instanceId, input);

    public async Task<WorkflowRuntimeStatus> GetWorkflowStatusAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        var state = await inner.GetWorkflowStateAsync(instanceId);
        return state.RuntimeStatus;
    }
}
