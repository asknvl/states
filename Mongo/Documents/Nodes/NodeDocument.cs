using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Nodes
{
    public sealed record NodeDocument
    {
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        [BsonElement("id")]
        public Guid Id { get; init; }

        [BsonElement("type")]
        public string Type { get; init; } = default!;

        [BsonElement("position")]
        public PositionDocument Position { get; init; } = default!;

        [BsonElement("data")]
        public NodeDataDocument Data { get; init; } = default!;
    }

    public sealed record PositionDocument
    {
        [BsonElement("x")]
        public int X { get; init; }

        [BsonElement("y")]
        public int Y { get; init; }
    }

    [BsonDiscriminator("nodeData")]
    [BsonKnownTypes(typeof(StartNodeDataDocument),
        typeof(SendPresetNodeDataDocument),
        typeof(ManageTagNodeDataDocument))]
    public abstract record NodeDataDocument
    {
        [BsonElement("label")]
        public string Label { get; init; } = default!;
    }

}
