using states.Services.FunnelService.Application;

namespace states.Mongo.Documents.Nodes
{
    public class SendMessageNode : Node
    {
        public SendMessageNode() : base(NodeType.Message, new MessageNodeData())
        {
        }
    }

    public class MessageNodeData : NodeData
    {        
        
    }
}
