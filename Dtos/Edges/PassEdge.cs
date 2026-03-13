using states.Services.FunnelService.Application;

namespace states.Dtos.Edges
{
    public sealed record PassEdge(
        Guid Id,        
        Guid Source,
        Guid Target
    ) : Edge(
        Id: Id,        
        Source: Source,
        Target: Target
    );
}
