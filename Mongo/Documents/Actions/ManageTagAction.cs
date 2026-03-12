using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Actions
{
    public class ManageTagAction : Action
    {
        [BsonRepresentation(BsonType.String)]
        [BsonElement("operation")]
        public TagOperation Operation { get; set; }

        [BsonElement("tagId")]
        public Guid TagId { get; set; }

        [BsonElement("replacementTagId")]
        public Guid? ReplacementTagId { get; set; }

        public ManageTagAction() : base(ActionType.ManageTag)
        {
        }
    }
}
