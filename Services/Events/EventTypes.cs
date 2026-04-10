namespace states.Services.Events
{
    public static class EventTypes
    {
        public const string BotActivationStatusChanged = "subscription.bot.status.changed";
        public const string IncomingMessageSignal = "signal.message.new";
        public const string ChatDeleted = "chat.deleted";
    }
}
