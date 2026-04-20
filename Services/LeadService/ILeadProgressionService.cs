using states.Dtos.Leads;

namespace states.Services.LeadService;

public interface ILeadProgressionService
{
    Task EnterFunnel(EnterFunnelRequest request, CancellationToken ct);
    Task TransitionToNextNode(Guid leadStateId, CancellationToken ct);
    Task ClearLeadStateByChat(Guid tenantId, Guid chatId);
    Task SetLeadFlowAndNode(Guid tenantId, string leadId, Guid flowId, Guid nodeId, CancellationToken ct);
    Task SetLeadStatus(Guid tenantId, string leadId, LeadFunnelStatus status, CancellationToken ct);
}
