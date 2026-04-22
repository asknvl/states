using states.Dtos.Funnels;

namespace states.Services.Events.Producer.Payloads
{
    public record LeadTagChangedPayload(
        Guid TenantId,
        Guid SpaceId,
        Guid BotId,
        Guid ChatId,
        string LeadId,
        List<Tag> Tags,
        long Version
    ) : LeadStateChangeEventPayloadBase(
        TenantId: TenantId,
        SpaceId: SpaceId,
        BotId: BotId,
        ChatId: ChatId,
        LeadId: LeadId,
        Version: Version);
    
}
