using Microsoft.AspNetCore.Mvc;
using states.Dtos.LeadEvents;
using states.Services.LeadEventsService.Application;
using Swashbuckle.AspNetCore.Annotations;

namespace states.Controllers
{
    [ApiController]
    [Route("/lead-events")]
    public class LeadEventsController : ControllerBase
    {
        private readonly ILeadEventsApplicationService leadEventsApplicationService;

        public LeadEventsController(ILeadEventsApplicationService leadEventsApplicationService)
        {
            this.leadEventsApplicationService = leadEventsApplicationService;
        }

        [HttpGet("{tenantId:guid}/spaces/{spaceId:guid}/leads/{leadId}")]
        [SwaggerOperation(Summary = "Returns all lead events for a lead")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyCollection<LeadEventBaseDto>>> GetLeadEvents(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid spaceId,
            [FromRoute] string leadId,
            CancellationToken ct)
        {
            var events = await leadEventsApplicationService.GetLeadEvents(tenantId, spaceId, leadId, ct);
            return Ok(events);
        }
    }
}
