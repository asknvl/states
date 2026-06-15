using states.Services.Events.Producer.Payloads.LeadState;

namespace states.Services.Events.Producer
{
    public record LeadStatusChangedEvent : Event<LeadStatusChangedPayload>
    {
        public LeadStatusChangedEvent(LeadStatusChangedPayload payload)
            : base(EventTypes.LeadStatusChanged, payload) { }
    }
}
