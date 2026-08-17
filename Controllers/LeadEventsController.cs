using Microsoft.AspNetCore.Mvc;
using states.Dtos.LeadEvents;
using states.Dtos.LeadEvents.Examples;
using states.Services.LeadEventsService.Application;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

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
        [SwaggerOperation(Summary = "Returns lead events for a lead filtered by event type")]
        [SwaggerRequestExample(typeof(GetLeadEventsRequest), typeof(GetLeadEventsRequestExample))]
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

        [HttpPost("import")]
        [SwaggerOperation(
            Summary = "Imports a batch of historical lead events migrated from an external service",
            Description = "Only Registration/Sale/Resale are supported — other event types are counted as Skipped. " +
                          "Idempotent by ExternalEventId — a repeated import of the same event is counted as NotImported, not an error. " +
                          "CALL ORDER: the lead's FunnelLeadState must already exist (POST /lead-migration/materialize) before " +
                          "importing its events — Sale/Resale deposits are only relayed to tgengine (LeadDepositChangedEvent) for " +
                          "an existing lead state; importing events first silently leaves tgengine's deposit totals stale.")]
        [ProducesResponseType(typeof(ImportLeadEventsResultDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ImportLeadEventsResultDto>> ImportEvents(
            [FromBody] ImportLeadEventsRequestDto request,
            CancellationToken ct)
        {
            var result = await leadEventsApplicationService.ImportEvents(request, ct);
            return Ok(result);
        }
    }
}
