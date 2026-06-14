namespace states.Dtos.Edges
{
    public sealed record AiRouterEdge(
       Guid Id,
       Guid Source,
       Guid Target,
       string Thesis,
       bool TriggerOnce
   ) : Edge(
       Id: Id,
       Source: Source,
       Target: Target
   );
}
