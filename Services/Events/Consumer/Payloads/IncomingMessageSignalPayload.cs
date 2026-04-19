namespace states.Services.Events.Consumer.Payloads
{
    public sealed record IncomingMessageSignalPayload(
        Guid TenantId,
        Guid BotId,
        Guid ChatId,        
        Guid GlobalId
    );

}
