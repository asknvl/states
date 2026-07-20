using states.Dtos.Leads;

namespace states.Services.LeadService;

public interface ILeadProgressionService
{    
    Task EnterFunnel(EnterFunnelRequest request, CancellationToken ct);
    Task TransitionToNextNode(Guid leadStateId, CancellationToken ct);
    Task ClearLeadStateByChat(Guid tenantId, Guid chatId);
    Task HandleIncomingSignal(Guid tenantId, Guid botId, Guid chatId, CancellationToken ct);
    Task SetLeadFunnelPosition(Guid tenantId, string leadId, Guid funnelId, Guid flowId, Guid nodeId, CancellationToken ct);
    Task SetLeadStatus(Guid tenantId, Guid chatId, LeadFunnelStatus status, CancellationToken ct);
    Task MarkLeadBlocked(Guid tenantId, Guid chatId, CancellationToken ct);
    Task ExecuteTransitionByEdge(Guid leadStateId, Guid edgeId, CancellationToken ct);
    Task UpdateLeadStateByChatId(Guid tenantId, Guid spaceId, Guid botId, Guid chatId, SetLeadStateRequest dto, CancellationToken ct);

    // Разовый бэкфилл: переотправляет депозитные события по всем лидам с депозитами, чтобы
    // заполнить lead_deposits в tgengine. Идемпотентно (upsert last-write-wins). Возвращает
    // число обработанных лидов.
    Task<int> BackfillDeposits(CancellationToken ct);
}
