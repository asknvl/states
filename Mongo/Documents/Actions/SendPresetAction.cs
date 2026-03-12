using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Actions
{
    public class SendPresetAction : Action
    {

        [BsonElement("presetId")]
        public Guid PresetId { get; set; }
        [BsonElement("delay")]
        public TimeSpan Delay { get; set; }
        [BsonElement("needPin")]
        public bool NeedPin { get; set; }

        public SendPresetAction() : base(ActionType.SendMessage)
        {
        }
    }
}
