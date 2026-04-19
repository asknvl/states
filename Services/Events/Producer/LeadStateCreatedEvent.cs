using states.Services.Events.Producer.Payloads;

namespace states.Services.Events.Producer
{
    public record LeadStateCreatedEvent : Event<LeadStateCreatedPayload>
    {
        public LeadStateCreatedEvent(LeadStateCreatedPayload payload)
            : base(EventTypes.LeadStateCreated, payload) { }
    }
}
