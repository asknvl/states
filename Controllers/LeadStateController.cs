using Microsoft.AspNetCore.Mvc;
using states.Dtos.Leads;
using states.Services.LeadService;
using Swashbuckle.AspNetCore.Annotations;

namespace states.Controllers
{
    [ApiController]
    [Route("/lead-states")]
    public class LeadStateController : ControllerBase
    {
        private readonly ILeadProgressionService leadProgressionService;

        public LeadStateController(ILeadProgressionService leadProgressionService)
        {
            this.leadProgressionService = leadProgressionService;
        }

        [HttpPatch("{tenantId:guid}/leads/{leadId}/node")]
        [SwaggerOperation(Summary = "Manually moves a lead to a specific flow and node")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SetFlowAndNode(
            [FromRoute] Guid tenantId,
            [FromRoute] string leadId,
            [FromBody] SetLeadFlowAndNodeRequest request,
            CancellationToken ct)
        {
            await leadProgressionService.SetLeadFlowAndNode(tenantId, leadId, request.FlowId, request.NodeId, ct);
            return NoContent();
        }

        [HttpPatch("{tenantId:guid}/leads/{leadId}/status")]
        [SwaggerOperation(Summary = "Manually sets a lead's funnel status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SetStatus(
            [FromRoute] Guid tenantId,
            [FromRoute] string leadId,
            [FromBody] LeadFunnelStatus status,
            CancellationToken ct)
        {
            await leadProgressionService.SetLeadStatus(tenantId, leadId, status, ct);
            return NoContent();
        }
    }
}
