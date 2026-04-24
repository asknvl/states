using states.Dtos.Leads;

namespace states.Services.LeadService;

public interface ILeadProgressionService
{
    Task EnterFunnel(EnterFunnelRequest request, CancellationToken ct);
    Task TransitionToNextNode(Guid leadStateId, CancellationToken ct);
    Task ClearLeadStateByChat(Guid tenantId, Guid chatId);
    Task SetLeadFunnelPosition(Guid tenantId, string leadId, Guid funnelId, Guid flowId, Guid nodeId, CancellationToken ct);
    Task SetLeadStatus(Guid tenantId, Guid chatId, LeadFunnelStatus status, CancellationToken ct);    
    Task UpdateLeadStateByChatId(Guid tenantId, Guid chatId, SetLeadStateRequest dto, CancellationToken ct);
}
