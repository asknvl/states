using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Actions;

public sealed class SendPushActionDocument : NodeActionDocument
{
    [BsonElement("presetId")]
    public Guid PresetId { get; set; }

    [BsonElement("delay")]
    public TimeSpan? Delay { get; set; }
    
    public SendPushActionDocument() : base(ActionType.SendPush)
    {
    }
}