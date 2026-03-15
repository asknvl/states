using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Actions;

public sealed class ManageTagActionDocument : NodeActionDocument
{
    [BsonRepresentation(BsonType.String)]
    [BsonElement("operation")]
    public TagOperation Operation { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    [BsonElement("tagId")]
    public Guid TagId { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    [BsonElement("replacementTagId")]
    public Guid? ReplacementTagId { get; set; }

    public ManageTagActionDocument() : base(ActionType.ManageTag)
    {
    }
}