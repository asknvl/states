using states.Services.Events.Producer.Payloads.LeadState;

namespace states.Services.Events.Producer
{
    public record LeadTagChangedEvent : Event<LeadTagsChangedPayload>
    {
        public LeadTagChangedEvent(LeadTagsChangedPayload payload)
            : base(EventTypes.LeadTagChanged, payload) { }
    }
}
