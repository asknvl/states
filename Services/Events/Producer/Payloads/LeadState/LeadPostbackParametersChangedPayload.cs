namespace states.Services.Events.Producer.Payloads.LeadState
{
    public record LeadPostbackParametersChangedPayload(
        Guid TenantId,
        Guid SpaceId,
        Guid BotId,
        Guid ChatId,
        string LeadId,
        Dictionary<string, string> PostbackParameters,
        long Version
    ) : LeadStateChangeEventPayloadBase(
            TenantId: TenantId,
            SpaceId: SpaceId,
            BotId: BotId,
            ChatId: ChatId,
            LeadId: LeadId,
            Version: Version);
}
