using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Edges
{
    [BsonDiscriminator(nameof(EdgeType.AiRouter))]
    public sealed record AiRouterEdgeDocument : EdgeDocument
    {
        [BsonElement("thesis")]
        public string Thesis { get; init; } = default!;
    }
}