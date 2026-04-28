namespace states.Dtos.Folders
{
    public sealed record ReorderFoldersDto(
        Guid TenantId,
        Guid? SpaceId,
        Guid FunnelId,
        IReadOnlyList<Guid> OrderedIds);
}
