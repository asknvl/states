using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Nodes
{

    [BsonDiscriminator(nameof(NodeType.ManageTag))]
    public sealed record ManageTagNodeData : NodeData
    {
        [BsonRepresentation(BsonType.String)]
        [BsonElement("operation")]
        public TagOperation Operation { get; init; }

        [BsonRepresentation(BsonType.String)]
        [BsonElement("tagId")]
        public Guid TagId { get; init; }

        [BsonRepresentation(BsonType.String)]
        [BsonElement("replacementTagId")]
        public Guid? ReplacementTagId { get; init; }
    }
}
