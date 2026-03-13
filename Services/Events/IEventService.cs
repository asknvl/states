namespace states.Services.Events
{
    public interface IEventService
    {
        Task Publish<TPayload>(Event<TPayload> @event, CancellationToken ct = default);
    }
}
