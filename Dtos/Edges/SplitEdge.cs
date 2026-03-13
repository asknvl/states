using states.Services.FunnelService.Application;

namespace states.Dtos.Edges
{
    public sealed record SplitEdge(
        Guid Id,        
        Guid Source,
        Guid Target,
        int Percentage
    ) : Edge(
        Id: Id,        
        Source: Source,
        Target: Target
    );    
}
