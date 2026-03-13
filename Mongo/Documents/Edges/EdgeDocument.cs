using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Edges
{
    [BsonDiscriminator("edge")]
    [BsonKnownTypes(typeof(PassEdgeDocument), typeof(SplitEdgeDocument))]
    public abstract record EdgeDocument
    {
        [BsonRepresentation(BsonType.String)]
        [BsonElement("id")]
        public Guid Id { get; init; }

        [BsonRepresentation(BsonType.String)]
        [BsonElement("source")]
        public Guid Source { get; init; }

        [BsonRepresentation(BsonType.String)]
        [BsonElement("target")]
        public Guid Target { get; init; }
    }

}
