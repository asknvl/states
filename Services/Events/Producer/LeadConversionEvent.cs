using states.Services.Events.Producer.Payloads.Conversions;

namespace states.Services.Events.Producer
{
    public record LeadConversionEvent : Event<LeadConversionPayload>
    {
        public LeadConversionEvent(LeadConversionPayload payload)
            : base(EventTypes.LeadConversion, payload) { }
    }
}
