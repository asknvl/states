using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Edges
{
    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(SplitEdge), typeof(AiRouterEdge), typeof(PassEdge))]
    public abstract class Edge
    {
        [BsonElement("id")]
        public string Id { get; set; }

        [BsonElement("source")]
        public string Source { get; set; }

        [BsonElement("target")]
        public string Target { get; set; }

        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public EdgeType Type { get; set; }

        protected Edge(EdgeType type)
        {
            Type = type;
        }
    }
}
