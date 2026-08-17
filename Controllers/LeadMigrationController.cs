using Microsoft.AspNetCore.Mvc;
using states.Dtos.Leads;
using states.Services.LeadMigration;
using Swashbuckle.AspNetCore.Annotations;

namespace states.Controllers
{
    /// <summary>
    /// Проактивный вход в воронку для лидов, мигрируемых из внешнего сервиса (см. migrator), до
    /// того как они реально написали боту. Отдельно от LeadStateController — служебная точка входа
    /// для процесса миграции, а не резолвинг живого сигнала.
    /// </summary>
    [ApiController]
    [Route("/lead-migration")]
    public class LeadMigrationController : ControllerBase
    {
        private readonly ILeadMigrationService leadMigrationService;

        public LeadMigrationController(ILeadMigrationService leadMigrationService)
        {
            this.leadMigrationService = leadMigrationService;
        }

        [HttpPost("materialize")]
        [Produces("application/json")]
        [SwaggerOperation(
            Summary = "Proactively enters a batch of migrated leads into their funnel position",
            Description = "Idempotent by the unique (tenantId, funnelId, leadId) index on lead_states — a repeated call " +
                          "for an already-entered lead is a safe no-op. ChatId/LeadId must already exist (tgengine/campaigns).")]
        [ProducesResponseType(typeof(MaterializeLeadStatesResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Materialize(
            [FromBody] MaterializeLeadStatesRequestDto request,
            CancellationToken ct)
        {
            var result = await leadMigrationService.MaterializeLeadStates(request, ct);
            return Ok(result);
        }

        [HttpDelete]
        [Produces("application/json")]
        [SwaggerOperation(
            Summary = "Full rollback: deletes every lead-state (and its action/push tasks, lead events) created by Materialize for a bot",
            Description = "No partial/selective rollback — everything with MigrationFrom=Chatterfy for (tenantId, botId) is removed. " +
                          "Intended for the maintenance window before the bot is started, when no real leads can exist yet.")]
        [ProducesResponseType(typeof(DeleteMigratedLeadStatesResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteMigrated(
            [FromQuery] Guid tenantId,
            [FromQuery] Guid botId,
            CancellationToken ct)
        {
            var result = await leadMigrationService.DeleteMigrated(tenantId, botId, ct);
            return Ok(result);
        }
    }
}
