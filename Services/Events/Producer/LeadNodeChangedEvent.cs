using states.Services.Events.Producer.Payloads;

namespace states.Services.Events.Producer
{
    public record LeadNodeChangedEvent : Event<LeadFunnelPositionChangedPayload>
    {
        public LeadNodeChangedEvent(LeadFunnelPositionChangedPayload payload)
            : base(EventTypes.LeadFunnelPositionChanged, payload) { }
    }
}
