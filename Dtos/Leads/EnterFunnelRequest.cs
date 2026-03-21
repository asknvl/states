namespace states.Dtos.Leads;

public sealed record EnterFunnelRequest(
    Guid TenantId,    
    Guid BotId,
    Guid ChatId,
    Guid FunnelId,
    Guid FlowId,
    Guid NodeId
);
