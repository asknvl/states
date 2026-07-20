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

        //[HttpPatch("{tenantId:guid}/leads/{leadId}/node")]
        //[SwaggerOperation(Summary = "Manually moves a lead to a specific flow and node")]
        //[ProducesResponseType(StatusCodes.Status204NoContent)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> SetLeadFunnelPosition(
        //    [FromRoute] Guid tenantId,
        //    [FromRoute] string leadId,
        //    [FromBody] SetLeadFlowAndNodeRequest request,
        //    CancellationToken ct)
        //{
        //    await leadProgressionService.SetLeadFunnelPosition(
        //        tenantId,
        //        leadId,
        //        request.FunnelId,
        //        request.FlowId,
        //        request.NodeId,
        //        ct);

        //    return NoContent();
        //}

        //[HttpPatch("{tenantId:guid}/chats/{chatId}/status")]
        //[SwaggerOperation(Summary = "Manually sets a lead's funnel status")]
        //[ProducesResponseType(StatusCodes.Status204NoContent)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> SetStatus(
        //    [FromRoute] Guid tenantId,
        //    [FromRoute] Guid chatId,
        //    [FromBody] LeadFunnelStatus status,
        //    CancellationToken ct)
        //{
        //    await leadProgressionService.SetLeadStatus(tenantId, chatId, status, ct);
        //    return NoContent();
        //}

        [HttpPatch("{tenantId:guid}/spaces/{spaceId:guid}/bots/{botId:guid}/chats/{chatId:guid}/state")]
        [SwaggerOperation(Summary = "Manually update lead state fields by chatId")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateLeadStateByChatId(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid spaceId,
            [FromRoute] Guid botId,
            [FromRoute] Guid chatId,
            [FromBody] SetLeadStateRequest dto,
            CancellationToken ct)
        {
            await leadProgressionService.UpdateLeadStateByChatId(
                tenantId,
                spaceId,
                botId,
                chatId,
                dto,
                ct);
            return NoContent();
        }

        // Разовая обслуживающая операция: переотправляет депозитные события по всем лидам с
        // депозитами, чтобы заполнить lead_deposits в tgengine (значения уже есть в Mongo, но
        // до внедрения события не доезжали в Postgres). Идемпотентно — можно вызывать повторно.
        // ВНИМАНИЕ: закрыть на уровне инфраструктуры (внутренняя сеть / шлюз), это не публичный API.
        [HttpPost("maintenance/backfill-deposits")]
        [SwaggerOperation(Summary = "One-off: re-emits deposit events for all leads with deposits to backfill tgengine")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> BackfillDeposits(CancellationToken ct)
        {
            var leadsProcessed = await leadProgressionService.BackfillDeposits(ct);
            return Ok(new { leadsProcessed });
        }
    }
}
