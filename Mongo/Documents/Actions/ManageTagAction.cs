using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Actions;

public sealed class ManageTagAction : NodeAction
{
    [BsonRepresentation(BsonType.String)]
    [BsonElement("operation")]
    public TagOperation Operation { get; set; }

    [BsonRepresentation(BsonType.String)]
    [BsonElement("tagId")]
    public Guid TagId { get; set; }

    [BsonRepresentation(BsonType.String)]
    [BsonElement("replacementTagId")]
    public Guid? ReplacementTagId { get; set; }

    public ManageTagAction() : base(ActionType.ManageTag)
    {
    }
}