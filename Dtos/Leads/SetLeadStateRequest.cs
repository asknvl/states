using states.Dtos.Funnels;
using states.Services.LeadService;

namespace states.Dtos.Leads
{
    public sealed record SetLeadStateRequest(                
            LeadFunnelStatus? Status,
            Guid? FunnelId,
            Guid? FlowId,
            Guid? NodeId,
            List<Tag>? Tags
        );
}
