using states.Services.Events.Producer.Payloads;

namespace states.Services.Events.Producer
{
    public record LeadNodeChangedEvent : Event<LeadNodeChangedPayload>
    {
        public LeadNodeChangedEvent(LeadNodeChangedPayload payload)
            : base(EventTypes.LeadNodeChanged, payload) { }
    }
}
