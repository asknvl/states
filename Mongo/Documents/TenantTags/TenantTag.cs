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

        [BsonElement("usages")]
        public List<Usage> Usages {get; set;} = [];
        
    }

    public class Usage
    {
        [BsonElement("spaceId")]
        public Guid SpaceId {get; set;}
        [BsonElement("funnelId")]
        public Guid FunnelId {get; set;}
    }
}
