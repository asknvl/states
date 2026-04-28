using states.Dtos.Folders;

namespace states.Services.Folders.Application
{
    public interface IFoldersApplicationService
    {
        Task<FolderDto> CreateFolder(FolderCreateDto dto, CancellationToken ct);
        Task<IReadOnlyCollection<FolderDto>> GetFolders(Guid tenantId, Guid? spaceId, Guid funnelId, CancellationToken ct);
        Task<FolderDto> UpdateFolder(Guid folderId, UpdateFolderDto dto, CancellationToken ct);
        Task DeleteFolder(Guid folderId, CancellationToken ct);
        Task<IReadOnlyCollection<FolderDto>> Reorder(ReorderFoldersDto dto, CancellationToken ct);
    }
}
