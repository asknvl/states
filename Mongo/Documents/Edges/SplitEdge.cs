using MongoDB.Bson.Serialization.Attributes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Edges
{
    public class SplitEdge : Edge
    {
        [BsonElement("percent")]
        public int Percent { get; set; }

        public SplitEdge() : base(EdgeType.Split)
        {
        }
    }
}
