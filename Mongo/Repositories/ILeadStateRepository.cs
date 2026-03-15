using states.Mongo.Documents;

namespace states.Mongo.Repositories;

public interface ILeadStateRepository
{
    Task<FunnelLeadState> Create(FunnelLeadState state, CancellationToken ct);
    Task<FunnelLeadState> Get(Guid leadStateId, CancellationToken ct);
    Task MoveToNode(Guid leadStateId, Guid edgeId, Guid nextNodeId, List<ActionStatusEntry> actions, CancellationToken ct);
    Task UpdateActionStatus(Guid leadStateId, Guid nodeId, Guid actionId, ActionStatus status, CancellationToken ct);
    Task<bool> AreAllActionsCompleted(Guid leadStateId, Guid nodeId, CancellationToken ct);
}
