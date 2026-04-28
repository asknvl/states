namespace states.Dtos.Folders
{
    public sealed record FolderDto(
            Guid Id,
            Guid TenantId,
            Guid SpaceId,
            Guid FunnelId,
            string Name,
            int Order,
            bool IsHidden,
            DateTime CreatedAt);
    
}
