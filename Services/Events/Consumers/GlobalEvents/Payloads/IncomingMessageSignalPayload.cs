namespace states.Services.Events.Consumers.GlobalEvents.Payloads
{
    public sealed record IncomingMessageSignalPayload(
        Guid TenantId,
        Guid BotId,
        Guid ChatId,        
        Guid GlobalId
    );

}
