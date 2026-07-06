using states.Dtos.LeadEvents;
using states.Mongo.Documents.LeadEvents;

namespace states.Services.LeadEventsService.Mapping
{
    public static class LeadEventMapper
    {
        public static LeadEventBaseDto ToDto(this LeadEventBaseDocument document)
        {
            return document switch
            {
                BotActivationEventDocument x => new BotActivationEventDto(
                    x.Id, x.TenantId, x.SpaceId, x.LeadId, x.EventId, x.Status, x.CreatedAt,
                    x.BotId),

                BotDeactivationEventDocument x => new BotDeactivationEventDto(
                    x.Id, x.TenantId, x.SpaceId, x.LeadId, x.EventId, x.Status, x.CreatedAt,
                    x.BotId),

                ChannelSubscriptionEventDocument x => new ChannelSubscriptionEventDto(
                    x.Id, x.TenantId, x.SpaceId, x.LeadId, x.EventId, x.Status, x.CreatedAt),

                ContactEventDocument x => new ContactEventDto(
                    x.Id, x.TenantId, x.SpaceId, x.LeadId, x.EventId, x.Status, x.CreatedAt),

                RegistrationLeadEvent x => new RegistrationEventDto(
                    x.Id, x.TenantId, x.SpaceId, x.LeadId, x.EventId, x.Status, x.CreatedAt,
                    x.CurrencyCode),

                ResaleLeadEvent x => new ResaleEventDto(
                    x.Id, x.TenantId, x.SpaceId, x.LeadId, x.EventId, x.Status, x.CreatedAt,
                    x.DepositAmount, x.CurrencyCode),

                SaleLeadEvent x => new SaleEventDto(
                    x.Id, x.TenantId, x.SpaceId, x.LeadId, x.EventId, x.Status, x.CreatedAt,
                    x.DepositAmount, x.CurrencyCode),

                _ => throw new NotSupportedException($"Unsupported lead event document type: {document.GetType().Name}")
            };
        }
    }
}
