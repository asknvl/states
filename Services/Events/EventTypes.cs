namespace states.Services.Events
{
    public static class EventTypes
    {
        //incoming
        public const string BotActivationStatusChanged = "subscription.bot.status.changed";
        public const string IncomingMessageSignal = "signal.message.new";
        public const string ChatDeleted = "chat.deleted";

        //outcoming
        public const string LeadStateCreated = "states.lead.created";
        public const string LeadNodeChanged = "states.lead.node.changed";
        public const string LeadStatusChanged = "states.lead.status.changed";
        public const string LeadTagChanged = "states.lead.tag.changed";


    }
}
