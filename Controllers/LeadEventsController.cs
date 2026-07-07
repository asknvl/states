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

        [HttpPost("filter")]
        [SwaggerOperation(Summary = "Returns lead events for a lead, optionally filtered by event type")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyCollection<LeadEventBaseDto>>> GetLeadEvents(
            [FromBody] GetLeadEventsRequest request,
            CancellationToken ct)
        {
            var events = await leadEventsApplicationService.GetLeadEvents(
                request.TenantId,
                request.SpaceId,
                request.LeadId,
                request.EventTypes,
                ct);
            return Ok(events);
        }

        [HttpGet("event-types")]
        [SwaggerOperation(Summary = "Returns all lead event types")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IReadOnlyCollection<LeadEventTypes>> GetLeadEventTypes()
        {
            var eventTypes = leadEventsApplicationService.GetLeadEventTypes();
            return Ok(eventTypes);
        }
    }
}
