using MongoDB.Bson.Serialization.Attributes;

namespace states.Mongo.Documents.LeadEvents
{
    [BsonDiscriminator("leadEvent")]
    [BsonKnownTypes(typeof(BotActivationEventDocument))]
    public abstract record LeadEventBaseDocument
    {
        public Guid TenantId { get; set; }
        public Guid SpaceId { get; set; }
        public Guid LeadId { get; set; }


        [BsonElement("createdAt")]
        DateTime CreatedAt { get; init; }
    }
}
