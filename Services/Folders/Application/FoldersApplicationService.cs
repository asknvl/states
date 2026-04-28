using states.Dtos.Folders;
using states.Mongo.Repositories;
using states.Services.Folders.Mapping;

namespace states.Services.Folders.Application
{
    public class FoldersApplicationService(
        IFoldersRepository foldersRepository) : IFoldersApplicationService
    {
        public async Task<FolderDto> CreateFolder(FolderCreateDto dto, CancellationToken ct)
        {
            var document = await foldersRepository.Create(
                dto.TenantId, dto.SpaceId, dto.FunnelId, dto.Name, dto.Order,
                isHidden: false, ct);
            return document.ToDto();
        }

        public async Task<IReadOnlyCollection<FolderDto>> GetFolders(
            Guid tenantId, Guid? spaceId, Guid funnelId, CancellationToken ct)
        {
            var documents = await foldersRepository.GetFolders(tenantId, spaceId, funnelId, ct);
            return documents.Select(d => d.ToDto()).ToList();
        }

        public async Task<FolderDto> UpdateFolder(Guid folderId, UpdateFolderDto dto, CancellationToken ct)
        {
            var document = await foldersRepository.Update(folderId, dto.Name, dto.Order, ct);
            return document.ToDto();
        }

        public async Task DeleteFolder(Guid folderId, CancellationToken ct)
        {
            await foldersRepository.Delete(folderId, ct);
        }

        public async Task<IReadOnlyCollection<FolderDto>> Reorder(ReorderFoldersDto dto, CancellationToken ct)
        {
            var documents = await foldersRepository.Reorder(
                dto.TenantId, dto.SpaceId, dto.FunnelId, dto.OrderedIds, ct);
            return documents.Select(d => d.ToDto()).ToList();
        }
    }
}
