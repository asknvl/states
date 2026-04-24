namespace states.Dtos.Leads;

public sealed record SetLeadFlowAndNodeRequest(
    Guid FunnelId,
    Guid FlowId,
    Guid NodeId);
