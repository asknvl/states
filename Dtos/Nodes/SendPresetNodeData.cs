using states.Dtos.Actions;

namespace states.Dtos.Nodes
{
    public sealed record SendPresetNodeData(
        string Label,        
        List<SendPresetAction> Actions
    ) : NodeData(
        Label
    );

}
