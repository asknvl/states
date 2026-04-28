using states.Dtos.Folders;
using states.Mongo.Documents.Folders;
using System.Runtime.CompilerServices;

namespace states.Services.Folders.Mapping
{
    public static class FolderMapper
    {
        public static FolderDto ToDto(this FolderDocument document)
        {
            return new FolderDto(
                Id: document.Id,
                TenantId: document.TenantId,
                SpaceId: document.SpaceId,
                FunnelId: document.FunnelId,
                Name: document.Name,
                Order: document.Order,
                IsHidden: document.IsHidden,
                CreatedAt: document.CreatedAt);
        }
    }
}
