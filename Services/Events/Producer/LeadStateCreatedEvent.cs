using states.Services.Events.Producer.Payloads.LeadState;

namespace states.Services.Events.Producer
{
    public record LeadStateCreatedEvent : Event<LeadStateCreatedPayload>
    {
        public LeadStateCreatedEvent(LeadStateCreatedPayload payload)
            : base(EventTypes.LeadStateCreated, payload) { }
    }
}
