namespace states.Services.Events.Producer.Payloads
{
    public record LeadNodeChangedPayload(        
        Guid TenantId,
        Guid SpaceId,
        Guid BotId,
        Guid ChatId,
        string LeadId,
        Guid NodeId,
        string? NodeLabel,
        long Version
    ) : LeadStateChangeEventPayloadBase(
        TenantId: TenantId,
        SpaceId: SpaceId,
        BotId: BotId,
        ChatId: ChatId,
        LeadId: LeadId,
        Version: Version);

}
