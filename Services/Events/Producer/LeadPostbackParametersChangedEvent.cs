using states.Services.Events.Producer.Payloads.LeadState;

namespace states.Services.Events.Producer
{
    public record LeadPostbackParametersChangedEvent : Event<LeadPostbackParametersChangedPayload>
    {
        public LeadPostbackParametersChangedEvent(LeadPostbackParametersChangedPayload payload)
            : base(EventTypes.LeadPostbackParametersChanged, payload) { }
    }
}
