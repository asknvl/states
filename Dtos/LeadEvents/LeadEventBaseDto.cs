using System.Text.Json.Serialization;
using states.Services.LeadEventsService.Application;

namespace states.Dtos.LeadEvents
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "eventType")]
    [JsonDerivedType(typeof(BotActivationEventDto), "BotActivation")]
    [JsonDerivedType(typeof(BotDeactivationEventDto), "BotDeactivation")]
    [JsonDerivedType(typeof(ChannelSubscriptionEventDto), "ChannelSubscribtion")]
    [JsonDerivedType(typeof(ContactEventDto), "Contact")]
    [JsonDerivedType(typeof(RegistrationEventDto), "Registration")]
    [JsonDerivedType(typeof(SaleEventDto), "Sale")]
    [JsonDerivedType(typeof(ResaleEventDto), "Resale")]
    public abstract record LeadEventBaseDto(
        Guid Id,
        Guid TenantId,
        Guid SpaceId,
        string LeadId,
        Guid EventId,
        LeadEventStatus Status,
        DateTime CreatedAt);
}
