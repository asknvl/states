using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Nodes
{
    [BsonDiscriminator(nameof(NodeType.Start))]
    public sealed record StartNodeDataDocument : NodeDataDocument
    {
    }
}
