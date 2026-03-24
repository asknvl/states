using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Mongo.Documents.Actions;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Nodes
{

    [BsonDiscriminator(nameof(NodeType.ManageTag))]
    public sealed record ManageTagNodeDataDocument : NodeDataDocument
    {
        [BsonElement("actions")]
        public List<ManageTagActionDocument> Actions { get; init; } = [];
    }
}
