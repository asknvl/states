using states.Services.Events.Producer.Payloads;

namespace states.Services.Events.Producer
{
    public record LeadTranslatorChangedEvent : Event<LeadTranslatorChangedPayload>
    {
        public LeadTranslatorChangedEvent(LeadTranslatorChangedPayload payload)
            : base(EventTypes.LeadTranslatorChanged, payload) { }
    }
}
