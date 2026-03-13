using states.Logging;

namespace states.Services.Events
{
    public class LogEventService : IEventService
    {
        private readonly ILogger logger;

        public LogEventService(ILogger<LogEventService> logger)
        {
            this.logger = logger;
        }

        public async Task Publish<TPayload>(Event<TPayload> @event, CancellationToken ct = default)
        {
            var notificationContext = logger.Notifiacation(@event);
            logger.LogInformation("Notification occured");
            await Task.CompletedTask;
        }
    }
}
