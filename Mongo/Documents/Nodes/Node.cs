using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Nodes
{
    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(StartNode))]
    [BsonKnownTypes(typeof(SendMessageNode))]
    public abstract class Node
    {        
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }

        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public NodeType Type { get; set; }
        
        [BsonElement("position")]
        public Position Position { get; set; }

        [BsonElement("data")]
        public NodeData Data { get; set; }

        public Node(NodeType type, NodeData data)
        {
            Type = type;
            Data = data;
        }
    }

    public class Position
    {
        [BsonElement("x")]
        public int X { get; set; }
        [BsonElement("y")]
        public int Y { get; set; }
    }


    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(StartNodeData))]
    public abstract class NodeData
    {
        [BsonElement("data")]
        public List<Action> Actions { get; set; } = new();
    }
}
