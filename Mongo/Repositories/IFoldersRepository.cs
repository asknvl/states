using states.Mongo.Documents.Folders;

namespace states.Mongo.Repositories
{
    public interface IFoldersRepository
    {
        Task<FolderDocument> Create(
            Guid tenantId,
            Guid spaceId,
            Guid funnelId,
            string name,
            int order,
            bool isHidden,
            CancellationToken ct = default);

        Task<IReadOnlyCollection<FolderDocument>> GetFolders(
            Guid tenantId,
            Guid? spaceId,
            Guid funnelId,
            CancellationToken ct = default);

        Task<FolderDocument> Update(
            Guid folderId,
            string? name,
            int? order,
            CancellationToken ct = default);

        Task Delete(Guid folderId, CancellationToken ct = default);

        Task<IReadOnlyCollection<FolderDocument>> Reorder(
            Guid tenantId,
            Guid? spaceId,
            Guid funnelId,
            IReadOnlyList<Guid> orderedIds,
            CancellationToken ct = default);
    }
}
