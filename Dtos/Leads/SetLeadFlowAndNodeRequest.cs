namespace states.Dtos.Leads;

public sealed record SetLeadFlowAndNodeRequest(Guid FlowId, Guid NodeId);
