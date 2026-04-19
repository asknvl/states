namespace states.Services.Events.Producer
{
    public interface IEventService
    {
        Task Publish<TPayload>(Event<TPayload> @event, CancellationToken ct = default);
    }
}
