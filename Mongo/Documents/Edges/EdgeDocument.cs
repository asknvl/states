using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Edges
{
    [BsonDiscriminator("edge")]
    [BsonKnownTypes(typeof(PassEdgeDocument), typeof(SplitEdgeDocument), typeof(AiRouterEdgeDocument))]
    public abstract record EdgeDocument
    {
        [BsonElement("id")]
        public Guid Id { get; init; }

        [BsonElement("source")]
        public Guid Source { get; init; }

        [BsonElement("target")]
        public Guid Target { get; init; }
    }

}
