using states.Mongo.Documents;
using states.Services.FunnelService.Application;
using states.Services.LeadService;

namespace states.Mongo.Repositories;

public interface ILeadStateRepository
{
    Task<FunnelLeadState> CreateLeadState(FunnelLeadState state, CancellationToken ct);
    Task<FunnelLeadState?> GetLeadState(Guid leadStateId, CancellationToken ct);
    Task<FunnelLeadState?> GetLeadStateByChatId(Guid tenantId, Guid botId, Guid chatId, CancellationToken ct);
    Task<FunnelLeadState?> GetLeadStateByChatId(Guid tenantId, Guid chatId, CancellationToken ct);
    Task<FunnelLeadState?> GetLeadStateByLeadId(Guid tenantId, string leadId, CancellationToken ct);
    Task MoveToNode(Guid leadStateId, Guid edgeId, Guid nextNodeId, List<ActionStatusEntry> actions, CancellationToken ct);
    Task SetFlowAndNode(Guid leadStateId, Guid flowId, Guid nodeId, List<ActionStatusEntry> actions, CancellationToken ct);
    Task UpdateActionStatus(Guid leadStateId, Guid nodeId, Guid actionId, ActionStatus status, CancellationToken ct, string? errorMessage = null);
    Task UpdateLeadStateStatus(Guid leadStateId, LeadFunnelStatus status, CancellationToken ct);
    Task ManageTag(Guid leadStateId, TagOperation operation, Guid tagId, Guid? replacementTagId, CancellationToken ct);
    Task<bool> AreAllActionsCompleted(Guid leadStateId, Guid nodeId, CancellationToken ct);
    Task Delete(Guid leadStateId, CancellationToken ct);
}
