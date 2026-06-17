namespace states.Services.Events.Consumers.LeadPostbackEvents;

public interface IPostbackEventProcessor
{
    Task Process(string eventType, string rawPayload, CancellationToken ct);
}
