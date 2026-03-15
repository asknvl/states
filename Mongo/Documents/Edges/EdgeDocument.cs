using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Edges
{
    [BsonDiscriminator("edge")]
    [BsonKnownTypes(typeof(PassEdgeDocument), typeof(SplitEdgeDocument))]
    public abstract record EdgeDocument
    {
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        [BsonElement("id")]
        public Guid Id { get; init; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        [BsonElement("source")]
        public Guid Source { get; init; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        [BsonElement("target")]
        public Guid Target { get; init; }
    }

}
