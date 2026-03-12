using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Actions
{
    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(ManageTagAction))]
    [BsonKnownTypes(typeof(SendPresetAction))]
    public abstract class Action
    {
        [BsonElement("id")]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.String)]
        [BsonElement("type")]
        public ActionType Type { get; set; }

        public Action(ActionType type)
        {
            Type = type;
        }
    }
}