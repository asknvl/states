using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Edges
{
    [BsonDiscriminator(nameof(EdgeType.Pass))]
    public sealed record PassEdgeDocument : EdgeDocument
    {
    }
}
