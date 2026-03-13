using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Edges
{
    [BsonDiscriminator(nameof(EdgeType.Split))]
    public sealed record SplitEdge : Edge
    {
        [BsonElement("percentage")]
        public int Percentage { get; init; }
    }
}
