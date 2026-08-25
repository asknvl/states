using states.Services.FunnelService.Application;
using states.Services.LeadService;

namespace states.Services.Events.Producer.Payloads.LeadState
{
    public record LeadStateCreatedPayload(
        Guid TenantId,
        Guid SpaceId,
        Guid BotId,
        Guid ChatId,
        string LeadId,
        Guid? CampaignId,
        string? CampaignName,
        string? SourceId,
        string? SourceName,
        Guid? FunnelId,
        string? FunnelName,
        Guid? FlowId,
        string? FlowName,
        Guid? NodeId,
        string? NodeLabel,
        LeadFunnelStatus Status,
        bool IsInputTranslatorOn,
        bool IsOutputTranslatorOn,
        RecognitionType PhotoRecognition,
        RecognitionType VideoRecognition,
        RecognitionType VoiceRecognition,
        long Version,

        // Начальные postback-параметры (custom fields миграции) и теги. Аддитивные поля:
        // старые консьюмеры незнакомые свойства игнорируют, null/пусто — ничего не делать.
        // Отдельные LeadPostbackParametersChanged/LeadTagChanged при создании не шлются.
        Dictionary<string, string>? PostbackParameters = null,
        List<states.Dtos.Funnels.Tag>? Tags = null
    ) : LeadStateChangeEventPayloadBase(
            TenantId: TenantId,
            SpaceId: SpaceId,
            BotId: BotId,
            ChatId: ChatId,
            LeadId: LeadId,
            Version: Version);
}
