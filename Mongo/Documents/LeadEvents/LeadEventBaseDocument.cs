using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.LeadEventsService.Application;

namespace states.Mongo.Documents.LeadEvents
{
    [BsonDiscriminator("leadEvent")]
    [BsonIgnoreExtraElements(Inherited = true)]
    [BsonKnownTypes(
        typeof(BotActivationEventDocument),
        typeof(BotDeactivationEventDocument),
        typeof(ChannelSubscriptionEventDocument),
        typeof(ContactEventDocument),
        typeof(RegistrationLeadEvent),
        typeof(ResaleLeadEvent),
        typeof(SaleLeadEvent))]
    public abstract record LeadEventBaseDocument
    {
        public Guid Id { get; set; }


        [BsonElement("tenantId")]
        public Guid TenantId { get; set; }
        [BsonElement("spaceId")]
        public Guid SpaceId { get; set; }
        [BsonElement("leadId")]
        public string LeadId { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
