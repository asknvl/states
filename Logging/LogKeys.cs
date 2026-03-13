using states.Services.Events;

namespace states.Logging
{
    public static class LogKeys
    {

        public const string Tag = "tag";

        public const string TenantId = "tenantId";
        public const string SpaceId = "spaceId";
        public const string BotId = "botId";

        public const string ChatId = "chatId";        
        public const string PresetId = "presetId";

        public const string EventType = "eventType";

    }

    public sealed class Disposable : IDisposable
    {
        public static readonly IDisposable Empty = new Disposable();
        private Disposable() { }
        public void Dispose() { }
    }

    public static class LoggerScopes
    {

        public static IDisposable Notifiacation<TPayload>(this ILogger logger, Event<TPayload> @event)
        {
            try
            {

                return logger.BeginScope(new Dictionary<string, object>
                {
                    [LogKeys.Tag] = "notification",
                    [LogKeys.EventType] = @event.Type
                    
                }) ?? Disposable.Empty;

            }
            catch
            {
                return Disposable.Empty;
            }
        }

        public static IDisposable Presets(this ILogger logger, Guid tenantId, Guid botId, Guid presetId)
        {
            try
            {
                return logger.BeginScope(new Dictionary<string, object>
                {
                    [LogKeys.Tag] = "presets",
                    [LogKeys.TenantId] = tenantId,
                    [LogKeys.BotId] = botId,
                    [LogKeys.PresetId] = presetId

                }) ?? Disposable.Empty;
            }
            catch
            {
                return Disposable.Empty;
            }
        }
    }
}
