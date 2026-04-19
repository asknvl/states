namespace states.Services.Events.Producer.Payloads
{
    public record LeadNodeChangedPayload(
        Guid LeadStateId,
        Guid TenantId,
        Guid ChatId,
        Guid NodeId,
        string? NodeLabel,
        long Version
    );
}
