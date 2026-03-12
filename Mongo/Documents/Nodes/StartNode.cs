using states.Mongo.Documents.Nodes;
using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Nodes
{
    public class StartNode : Node
    {
        public StartNode() : base(NodeType.Start, new StartNodeData())
        {
        }
    }

    public class StartNodeData : NodeData
    {        
    }
}
