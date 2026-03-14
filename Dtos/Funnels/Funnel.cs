using states.Mongo.Documents;

namespace states.Dtos.Funnels
{
    public sealed record Funnel(            
            Guid Id,
            Guid TenantId,
            Guid SpaceId,
            Guid BotId,
            string Name,
            string? Description,
            List<Tag> Tags,
            List<Flow> Flows,
            bool? IsActive
        );
    
}
