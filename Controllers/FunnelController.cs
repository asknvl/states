using Microsoft.AspNetCore.Mvc;
using states.Dtos.Funnels;
using states.Dtos.Funnels.Examples;
using states.Services.FunnelService.Application;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace states.Controllers
{
    [ApiController]
    [Route("/funnels")]
    public class FunnelController : ControllerBase
    {
        private readonly IFunnelsApplicationService funnelsApplicationService;
        private readonly ILogger<FunnelController> logger;

        public FunnelController(
            IFunnelsApplicationService funnelsApplicationService,
            ILogger<FunnelController> logger)
        {
            this.funnelsApplicationService = funnelsApplicationService;
            this.logger = logger;
        }

        #region funnel

        [HttpPost]
        [Produces("application/json")]
        [SwaggerOperation(Summary = "Creates a new funnel")]
        [SwaggerRequestExample(typeof(Funnel), typeof(FunnelExample))]
        [ProducesResponseType(typeof(Funnel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] Funnel dto, CancellationToken ct)
        {
            var result = await funnelsApplicationService.Create(dto, ct);
            return Ok(result);
        }

        [HttpPut("{funnelId:guid}")]
        [Produces("application/json")]
        [SwaggerOperation(Summary = "Updates a funnel")]
        [SwaggerRequestExample(typeof(Funnel), typeof(FunnelExample))]
        [ProducesResponseType(typeof(Funnel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update([FromRoute] Guid funnelId, [FromBody] Funnel dto, CancellationToken ct)
        {
            var result = await funnelsApplicationService.Update(dto with { Id = funnelId }, ct);
            return Ok(result);
        }

        [HttpGet("{funnelId:guid}")]
        [Produces("application/json")]
        [SwaggerOperation(Summary = "Gets a funnel by id")]
        [ProducesResponseType(typeof(Funnel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromRoute] Guid funnelId, CancellationToken ct)
        {
            var result = await funnelsApplicationService.Get(funnelId, ct);
            return Ok(result);
        }

        [HttpGet]
        [Produces("application/json")]
        [SwaggerOperation(Summary = "Gets all funnels for a bot")]
        [ProducesResponseType(typeof(IReadOnlyCollection<Funnel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromQuery] Guid tenantId, [FromQuery] Guid botId, CancellationToken ct)
        {
            var result = await funnelsApplicationService.Get(tenantId, botId, ct);
            return Ok(result);
        }

        [HttpPatch("{funnelId:guid}/active")]
        [SwaggerOperation(Summary = "Sets the active state of a funnel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SetIsActive([FromRoute] Guid funnelId, [FromBody] bool isActive, CancellationToken ct)
        {
            await funnelsApplicationService.SetIsActive(funnelId, isActive, ct);
            return NoContent();
        }

        #endregion

        #region tags

        [HttpGet("{funnelId:guid}/tags")]
        [Produces("application/json")]
        [SwaggerOperation(Summary = "Gets all tags of a funnel")]
        [ProducesResponseType(typeof(IReadOnlyCollection<Tag>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTags([FromRoute] Guid funnelId, CancellationToken ct)
        {
            var result = await funnelsApplicationService.GetTags(funnelId, ct);
            return Ok(result);
        }

        [HttpPost("{funnelId:guid}/tags")]
        [Produces("application/json")]
        [SwaggerOperation(Summary = "Adds a tag to a funnel")]
        [ProducesResponseType(typeof(IReadOnlyList<Tag>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddTag([FromRoute] Guid funnelId, [FromBody] Tag tag, CancellationToken ct)
        {
            var result = await funnelsApplicationService.AddTag(funnelId, tag, ct);
            return Ok(result);
        }

        [HttpPatch("{funnelId:guid}/tags/{tagId:guid}")]
        [Produces("application/json")]
        [SwaggerOperation(Summary = "Updates a tag in a funnel")]
        [ProducesResponseType(typeof(IReadOnlyList<Tag>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTag([FromRoute] Guid funnelId, [FromRoute] Guid tagId, [FromBody] string name, CancellationToken ct)
        {
            var result = await funnelsApplicationService.UpdateTag(funnelId, tagId, name, ct);
            return Ok(result);
        }

        [HttpDelete("{funnelId:guid}/tags/{tagId:guid}")]
        [Produces("application/json")]
        [SwaggerOperation(Summary = "Removes a tag from a funnel")]
        [ProducesResponseType(typeof(IReadOnlyList<Tag>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveTag([FromRoute] Guid funnelId, [FromRoute] Guid tagId, CancellationToken ct)
        {
            var result = await funnelsApplicationService.RemoveTag(funnelId, tagId, ct);
            return Ok(result);
        }

        #endregion
    }
}