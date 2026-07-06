using states.Services.Events.Producer.Payloads.LeadState;

namespace states.Services.Events.Producer
{
    public record LeadDepositChangedEvent : Event<LeadDepositChangedPayload>
    {
        public LeadDepositChangedEvent(LeadDepositChangedPayload payload)
            : base(EventTypes.LeadDepositChanged, payload) { }
    }
}
