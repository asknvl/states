namespace states.Services.Events.Producer.Payloads.LeadState
{
    public record LeadStateChangeEventPayloadBase(
            Guid TenantId,
            Guid SpaceId,
            Guid BotId,
            Guid ChatId,
            string LeadId,
            long Version);
}
