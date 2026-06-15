namespace states.Services.Events.Producer.Payloads.LeadState
{
    public record LeadTranslatorChangedPayload(
        Guid TenantId,
        Guid SpaceId,
        Guid BotId,
        Guid ChatId,
        string LeadId,
        bool IsInputTranslatorOn,
        bool IsOutputTranslatorOn,
        long Version
    ) : LeadStateChangeEventPayloadBase(
            TenantId: TenantId,
            SpaceId: SpaceId,
            BotId: BotId,
            ChatId: ChatId,
            LeadId: LeadId,
            Version: Version);
}
