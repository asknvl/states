using states.Services.Events.Producer.Payloads.LeadState;

namespace states.Services.Events.Producer
{
    public record LeadFunnelPositionChangedEvent : Event<LeadFunnelPositionChangedPayload>
    {
        public LeadFunnelPositionChangedEvent(LeadFunnelPositionChangedPayload payload)
            : base(EventTypes.LeadFunnelPositionChanged, payload) { }
    }
}
