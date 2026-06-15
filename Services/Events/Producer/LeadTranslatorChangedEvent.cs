using states.Services.Events.Producer.Payloads.LeadState;

namespace states.Services.Events.Producer
{
    public record LeadTranslatorChangedEvent : Event<LeadTranslatorChangedPayload>
    {
        public LeadTranslatorChangedEvent(LeadTranslatorChangedPayload payload)
            : base(EventTypes.LeadTranslatorChanged, payload) { }
    }
}
