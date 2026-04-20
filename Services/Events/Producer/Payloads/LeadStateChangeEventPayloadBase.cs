namespace states.Services.Events.Producer.Payloads
{
    public record LeadStateChangeEventPayloadBase(
            Guid TenantId,
            Guid SpaceId,
            Guid BotId,
            Guid ChatId,
            string LeadId,
            long Version);
}
