namespace states.Services.Events.Consumers.GlobalEvents.Payloads
{
    public record ChatDeletionPayload(
        Guid TenantId,
        Guid ChatId
    );    
}
