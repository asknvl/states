using states.Services.LeadService;

namespace states.Dtos.Leads
{
    public sealed record SetLeadStateRequest(                
            LeadFunnelStatus? Status,
            Guid? FlowId,
            Guid? NodeId,
            List<Guid>? Tags
        );
}
