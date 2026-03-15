using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Nodes
{

    [BsonDiscriminator(nameof(NodeType.ManageTag))]
    public sealed record ManageTagNodeDataDocument : NodeDataDocument
    {
        [BsonRepresentation(BsonType.String)]
        [BsonElement("operation")]
        public TagOperation Operation { get; init; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        [BsonElement("tagId")]
        public Guid TagId { get; init; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        [BsonElement("replacementTagId")]
        public Guid? ReplacementTagId { get; init; }
    }
}
