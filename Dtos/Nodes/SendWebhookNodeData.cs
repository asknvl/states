using states.Services.FunnelService.Application;
using states.Services.LeadService;

namespace states.Dtos.Nodes
{
    public sealed record SendWebhookNodeData(
        string Label,
        LeadFunnelStatus FinishStatus,
        string Url,
        WebhookMethodType MethodType
        ) : NodeData(
            Label: Label,
            FinishStatus: FinishStatus);

}
