using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Mongo.Documents.Edges;
using states.Mongo.Documents.Nodes;
using states.Services.FunnelService;

namespace states.Mongo.Documents
{
    public class FunnelDocument
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }

        [BsonElement("tenantId")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid TenantId { get; set; }

        [BsonElement("spaceId")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid SpaceId { get; set; }

        [BsonElement("botId")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid BotId { get; set; }

        [BsonElement("name")]
        public required string Name { get; set; }

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("tags")]
        public List<Tag> Tags { get; set; } = [];

        [BsonElement("flows")]
        public List<Flow> Flows { get; set; } = [];
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

    public class Tag
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }

        [BsonElement("name")]
        public required string Name { get; set; }
    }
    
}
