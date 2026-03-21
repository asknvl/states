namespace states.Services.Events.Consumer;

public interface IGlobalEventProcessor
{
    Task Process(string eventType, string rawPayload, CancellationToken ct);
}
