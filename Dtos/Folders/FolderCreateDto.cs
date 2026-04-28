namespace states.Dtos.Folders
{
    public sealed record FolderCreateDto(
            Guid TenantId,
            Guid SpaceId,
            Guid FunnelId,
            string Name,
            int Order);
    
}
