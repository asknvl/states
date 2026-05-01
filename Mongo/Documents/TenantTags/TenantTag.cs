using MongoDB.Bson.Serialization.Attributes;

namespace states.Mongo.Documents.TenantTags
{
    public class TenantTag
    {
        [BsonId]
        [BsonElement]
        public Guid Id { get; set; }
        [BsonElement("tenantId")]
        public Guid TenantId { get; set; }
        [BsonElement("tagId")]
        public Guid TagId { get; set; }
        [BsonElement("tagName")]
        public string TagName { get; set; }
        [BsonElement("usagesNumber")]
        public int UsagesNumber { get; set; }
        [BsonElement("usedInSpaces")]
        public List<Guid> UsedInSpaces { get; set; } = [];
        [BsonElement("usedInFunnels")]
        public List<Guid> UsedInFunnels { get; set; } = [];
    }

    public class Usage
    {
        public Guid SpaceId {get; set;}
        public Guid FunnelId {get; set;}
    }
}
