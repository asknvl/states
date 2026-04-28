using MongoDB.Bson.Serialization.Attributes;

namespace states.Mongo.Documents.Folders
{
    public class FolderDocument
    {
        [BsonId]
        [BsonElement("id")]
        public Guid Id { get; set; }

        [BsonElement("tenantId")]
        public Guid TenantId { get; set; }

        [BsonElement("spaceId")]
        public Guid SpaceId { get; set; }

        [BsonElement("funnelId")]
        public Guid FunnelId { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("order")]
        public int Order { get; set; }
        
        [BsonElement("isHidden")]
        public bool IsHidden {get; set;}

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
