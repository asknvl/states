namespace states.Services.Events.Consumer;

public interface IPostbackEventProcessor
{
    Task Process(string eventType, string rawPayload, CancellationToken ct);
}
