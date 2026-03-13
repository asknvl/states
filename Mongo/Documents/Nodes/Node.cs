using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Nodes
{
    public sealed record Node
    {
        [BsonRepresentation(BsonType.String)]
        [BsonElement("id")]
        public Guid Id { get; init; }

        [BsonElement("type")]
        public string Type { get; init; } = default!;

        [BsonElement("position")]
        public Position Position { get; init; } = default!;

        [BsonElement("data")]
        public NodeData Data { get; init; } = default!;
    }

    public sealed record Position
    {
        [BsonElement("x")]
        public int X { get; init; }

        [BsonElement("y")]
        public int Y { get; init; }
    }

    [BsonDiscriminator("nodeData")]
    [BsonKnownTypes(typeof(StartNodeData),
        typeof(SendPresetNodeData),
        typeof(ManageTagNodeData))]
    public abstract record NodeData
    {
        [BsonElement("label")]
        public string Label { get; init; } = default!;
    }

}
