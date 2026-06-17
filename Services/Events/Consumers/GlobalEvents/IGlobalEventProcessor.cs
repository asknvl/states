namespace states.Services.Events.Consumers.GlobalEvents;

public interface IGlobalEventProcessor
{
    Task Process(string eventType, string rawPayload, CancellationToken ct);
}
