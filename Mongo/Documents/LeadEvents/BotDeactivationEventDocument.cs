using MongoDB.Bson.Serialization.Attributes;

namespace states.Mongo.Documents.LeadEvents
{
    public sealed record BotDeactivationEventDocument : LeadEventBaseDocument
    {
        [BsonElement("botId")]
        public Guid BotId { get; set; }
    }
}
