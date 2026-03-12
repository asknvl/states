using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Edges
{
    public class PassEdge : Edge
    {
        public PassEdge() : base(EdgeType.Pass)
        {
        }
    }
}
