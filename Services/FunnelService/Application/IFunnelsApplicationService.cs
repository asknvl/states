using states.Dtos.Funnels;
using states.Dtos.Nodes;

namespace states.Services.FunnelService.Application
{
    public interface IFunnelsApplicationService
    {
        Task<FunnelDto> Create(FunnelCreateDto dto, CancellationToken ct);
        Task<FunnelDto> Update(Guid funnelId, FunnelUpdateDto dto, CancellationToken ct);
        Task<Funnel> Get(Guid funnelId, CancellationToken ct);
        Task<IReadOnlyCollection<FunnelDto>> Get(Guid tenantId, Guid? spaceId, Guid? botId, CancellationToken ct);
        Task<IReadOnlyCollection<FunnelShortDto>> GetShort(Guid tenantId, Guid? spaceId, CancellationToken ct);
        Task SetIsActive(Guid funnelId, bool isActive, CancellationToken ct);
        Task SetIsTranslatorOn(Guid funnelId, bool? isInputTranslatorOn, bool? isOutputTranslatorOn, CancellationToken ct);
        Task SetRecognition(Guid funnelId, RecognitionType? photoRecognition, RecognitionType? videoRecognition, RecognitionType? voiceRecognition, CancellationToken ct);
        Task SetDelays(Guid funnelId, int? readDelay, int? replyDelay, CancellationToken ct);
        Task SetAiModels(Guid funnelId, Guid? aiRouterModelPresetId, Guid? aiReplyModelPresetId, double? aiReplyTemperature, CancellationToken ct);

        Task<IReadOnlyCollection<Tag>> GetTagsByFunnel(Guid funnelId, CancellationToken ct);
        Task<IReadOnlyCollection<Tag>> GetTagsByTenant(Guid tenantId, Guid spaceId, CancellationToken ct);
        Task<IReadOnlyList<Tag>> AddTag(Guid funnelId, string name, CancellationToken ct);
        Task<IReadOnlyList<Tag>> RemoveTag(Guid funnelId, Guid tagId, CancellationToken ct);
        Task<IReadOnlyList<Tag>> UpdateTag(Guid funnelId, Guid tagId, string name, CancellationToken ct);

        Task<Flow> GetFlow(Guid funnelId, Guid flowId, CancellationToken ct);
        Task<IReadOnlyCollection<FlowShortDto>> GetFlowsShort(Guid funnelId, CancellationToken ct);
        Task<Flow> AddFlow(Guid funnelId, Flow flow, CancellationToken ct);
        Task<Flow> UpdateFlow(Guid funnelId, Flow flow, CancellationToken ct);
        Task RemoveFlow(Guid funnelId, Guid flowId, CancellationToken ct);

        Task<IReadOnlyCollection<NodeShortDto>> GetNodesShort(Guid funnelId, Guid flowId, CancellationToken ct);
    }
}