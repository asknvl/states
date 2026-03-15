using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Actions;

[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(typeof(ManageTagActionDocument))]
[BsonKnownTypes(typeof(SendPresetActionDocument))]
public abstract class NodeActionDocument
{
    [BsonElement("id")]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public ActionType Type { get; set; }

    protected NodeActionDocument(ActionType type)
    {
        Type = type;
    }
}