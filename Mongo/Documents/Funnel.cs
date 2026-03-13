using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Mongo.Documents.Edges;
using states.Mongo.Documents.Nodes;

namespace states.Mongo.Documents
{
    public class Funnel
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        [BsonElement("id")]
        public Guid Id { get; init; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        [BsonElement("tenantId")]
        public Guid TenantId { get; init; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        [BsonElement("spaceId")]
        public Guid SpaceId { get; init; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        [BsonElement("botId")]
        public Guid BotId { get; init; }

        [BsonElement("name")]
        public string Name { get; init; } = default!;

        [BsonElement("description")]
        public string? Description { get; init; }

        [BsonElement("tags")]
        public List<Tag> Tags { get; init; } = [];

        [BsonElement("flows")]
        public List<Flow> Flows { get; init; } = [];
    }

    public class Flow
    {
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }

        [BsonElement("name")]
        public required string Name { get; set; }

        [BsonElement("nodes")]
        public List<Node> Nodes { get; set; } = [];

        [BsonElement("edges")]
        public List<Edge> Edges { get; set; } = [];
    }

    public sealed record Tag
    {
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        [BsonElement("id")]
        public Guid Id { get; init; }

        [BsonElement("name")]
        public string Name { get; init; } = default!;
    }

}
