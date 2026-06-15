using states.Dtos.Funnels;
using states.Services.FunnelService.Application;

namespace states.Services.Events.Producer.Payloads
{
    public record LeadTagsChangedPayload(
        Guid TenantId,
        Guid SpaceId,
        Guid BotId,
        Guid ChatId,
        string LeadId,
        List<Tag> Tags,
        TagOperation Operation,
        Tag? Tag,
        long Version
    ) : LeadStateChangeEventPayloadBase(
        TenantId: TenantId,
        SpaceId: SpaceId,
        BotId: BotId,
        ChatId: ChatId,
        LeadId: LeadId,
        Version: Version);
    
}
