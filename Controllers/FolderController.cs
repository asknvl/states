using Microsoft.AspNetCore.Mvc;
using states.Dtos.Folders;
using states.Services.Folders.Application;

namespace states.Controllers
{
    [ApiController]
    [Route("/folders")]
    public class FoldersController(IFoldersApplicationService foldersApplicationService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<FolderDto>>> GetFolders(
            [FromQuery] Guid tenantId,
            [FromQuery] Guid? spaceId,
            [FromQuery] Guid funnelId,
            CancellationToken ct)
        {
            var folders = await foldersApplicationService.GetFolders(tenantId, spaceId, funnelId, ct);
            return Ok(folders);
        }

        [HttpPost]
        public async Task<ActionResult<FolderDto>> CreateFolder(
            [FromBody] FolderCreateDto dto,
            CancellationToken ct)
        {
            var folder = await foldersApplicationService.CreateFolder(dto, ct);
            return Ok(folder);
        }

        [HttpPatch("{folderId:guid}")]
        public async Task<ActionResult<FolderDto>> UpdateFolder(
            Guid folderId,
            [FromBody] UpdateFolderDto dto,
            CancellationToken ct)
        {
            var folder = await foldersApplicationService.UpdateFolder(folderId, dto, ct);
            return Ok(folder);
        }

        [HttpDelete("{folderId:guid}")]
        public async Task<ActionResult> DeleteFolder(Guid folderId, CancellationToken ct)
        {
            await foldersApplicationService.DeleteFolder(folderId, ct);
            return NoContent();
        }

        [HttpPatch("reorder")]
        public async Task<ActionResult<IReadOnlyCollection<FolderDto>>> Reorder(
            [FromBody] ReorderFoldersDto dto,
            CancellationToken ct)
        {
            var folders = await foldersApplicationService.Reorder(dto, ct);
            return Ok(folders);
        }
    }
}
