using Microsoft.AspNetCore.Mvc;
using states.Dtos.Funnels;
using states.Services.FunnelService.Application;
using Swashbuckle.AspNetCore.Annotations;

namespace states.Controllers
{
    [ApiController]
    [Route("/tags")]
    public class TagsController(IFunnelsApplicationService funnelsApplicationService) : ControllerBase
    {
        [HttpGet]
        [Produces("application/json")]
        [SwaggerOperation(Summary = "Gets all tags for a tenant filtered by space")]
        [ProducesResponseType(typeof(IReadOnlyCollection<Tag>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTagsByTenant(
            [FromQuery] Guid tenantId,
            [FromQuery] Guid spaceId,
            CancellationToken ct)
        {
            var result = await funnelsApplicationService.GetTagsByTenant(tenantId, spaceId, ct);
            return Ok(result);
        }
    }
}
