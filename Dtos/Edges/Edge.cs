using states.Services.FunnelService.Application;
using System.Text.Json.Serialization;

namespace states.Dtos.Edges
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "edgeType")]
    [JsonDerivedType(typeof(PassEdge), nameof(EdgeType.Pass))]
    [JsonDerivedType(typeof(SplitEdge), nameof(EdgeType.Split))]
    public abstract record Edge(
        Guid Id,        
        Guid Source,
        Guid Target
    );

    public abstract record Condition();
    
}
