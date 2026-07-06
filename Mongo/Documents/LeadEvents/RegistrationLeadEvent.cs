using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.LeadEventsService.Application;

namespace states.Mongo.Documents.LeadEvents
{
    public sealed record RegistrationLeadEvent : LeadEventBaseDocument
    {
        [BsonElement("eventId")]
        public Guid EventId { get; set; }

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]        
        public LeadEventStatus Status { get; set; }

        [BsonElement("currencyCode")]
        public string CurrencyCode { get; set; } = string.Empty;
    }
}
