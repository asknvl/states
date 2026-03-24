using states.Mongo.Documents.Nodes;
using states.Services.FunnelService.Application;
using System.Text.Json.Serialization;

namespace states.Dtos.Nodes
{   
    public record Node(
        Guid Id,                
        string Type,
        Position Position,
        NodeData Data
    );

    public record Position(
        double X,
        double Y
    );


    [JsonPolymorphic(TypeDiscriminatorPropertyName = "nodeType")]
    [JsonDerivedType(typeof(StartNodeData), nameof(NodeType.Start))]
    [JsonDerivedType(typeof(SendPresetNodeData), nameof(NodeType.SendPreset))]
    [JsonDerivedType(typeof(ManageTagNodeData), nameof(NodeType.ManageTag))]
    public abstract record NodeData(        
        string Label        
    );    

}
