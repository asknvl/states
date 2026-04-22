using states.Services.Events.Producer.Payloads;

namespace states.Services.Events.Producer
{
    public record LeadTagChangedEvent : Event<LeadTagChangedPayload>
    {
        public LeadTagChangedEvent(LeadTagChangedPayload payload)
            : base(EventTypes.LeadTagChanged, payload) { }
    }
}
