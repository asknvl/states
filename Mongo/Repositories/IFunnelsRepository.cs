using states.Mongo.Documents;
using states.Services.FunnelService.Application;

namespace states.Mongo.Repositories
{
    public interface IFunnelsRepository
    {
        Task Create(FunnelDocument document, CancellationToken ct);
        Task UpdateMetadata(Guid funnelId, string name, string? description, CancellationToken ct);

        Task<IReadOnlyCollection<FunnelDocument>> GetFunnels(Guid tenantId, Guid? spaceId, Guid? botId, CancellationToken ct);
        Task<FunnelDocument> Get(Guid funnelId);           

        Task AddFlow(Guid funnelId, FlowDocument flow, CancellationToken ct);
        Task UpdateFlow(Guid funnelId, FlowDocument flow, CancellationToken ct);
        Task RemoveFlow(Guid funnelId, Guid flowId, CancellationToken ct);

        Task<IReadOnlyCollection<TagDocument>> GetTags(Guid funnelId, CancellationToken ct);
        Task<IReadOnlyList<TagDocument>> AddTag(Guid funnelId, Guid tagId, string name, CancellationToken ct);
        Task<IReadOnlyList<TagDocument>> RemoveTag(Guid funnelId, Guid tagId, CancellationToken ct);
        Task<IReadOnlyList<TagDocument>> UpdateTag(Guid funnelId, Guid tagId, string name, CancellationToken ct);

        Task<IReadOnlyCollection<VariableDocument>> GetVariables(Guid funnelId, CancellationToken ct);
        Task<IReadOnlyList<VariableDocument>> AddVariable(Guid funnelId, VariableDocument variable, CancellationToken ct);
        Task<IReadOnlyList<VariableDocument>> RemoveVariable(Guid funnelId, Guid variableId, CancellationToken ct);
        Task<IReadOnlyList<VariableDocument>> UpdateVariable(Guid funnelId, Guid variableId, string macros, string value, CancellationToken ct);

        Task SetIsActiveState(Guid funnelId, bool isActive, CancellationToken ct);
        Task SetIsTranslatorOn(Guid funnelId, bool? isInputTranslatorOn, bool? isOutputTranslatorOn, CancellationToken ct);
        Task SetRecognition(Guid funnelId, RecognitionType? photoRecognition, RecognitionType? videoRecognition, RecognitionType? voiceRecognition, CancellationToken ct);
        Task SetDelays(Guid funnelId, int? readDelay, int? replyDelay, CancellationToken ct);
        Task SetAiModels(Guid funnelId, Guid? aiRouterModelPresetId, Guid? aiReplyModelPresetId, double? aiReplyTemperature, CancellationToken ct);
        Task SetAiPrompts(Guid funnelId, string? globalLegend, string? restrictions, string? responseStyle, CancellationToken ct);

        Task<IReadOnlyCollection<FunnelDocument>> GetAllActive(CancellationToken ct);
    }
}
