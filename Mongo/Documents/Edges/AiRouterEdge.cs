using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Edges
{
    public class AiRouterEdge : Edge
    {
        [BsonElement("thesis")]
        public string Thesis { get; set; }
        public AiRouterEdge() : base(EdgeType.AiRouter)
        {
        }
    }
}
