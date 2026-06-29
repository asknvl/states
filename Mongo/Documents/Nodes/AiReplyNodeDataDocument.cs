using MongoDB.Bson.Serialization.Attributes;
using states.Mongo.Documents.Actions;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Nodes
{
    [BsonDiscriminator(nameof(NodeType.AiReply))]
    public sealed record AiReplyNodeDataDocument : NodeDataDocument
    {
        [BsonElement("goal")]
        public string? Goal { get; init; }
        [BsonElement("requirements")]
        public string? Requirements { get; init; }
        [BsonElement("legend")]
        public string? Legend { get; init; }
        [BsonElement("additionalInfo")]
        public string? AdditionalInfo { get; init; }
        [BsonElement("pushes")]
        public List<SendPushActionDocument> Pushes { get; init; } = [];
    }
}
