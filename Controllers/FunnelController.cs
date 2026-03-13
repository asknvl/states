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
    public class FunnelController : Controller
    {
        private readonly IFunnelsApplicationService funnelsApplicationService;
        private readonly ILogger logger;

        public FunnelController(
            IFunnelsApplicationService funnelsApplicationService,
            ILogger<FunnelController> logger)
        {
            this.funnelsApplicationService = funnelsApplicationService;
            this.logger = logger;
        }

        [HttpPost]
        [Produces("application/json")]
        [SwaggerOperation(
            Summary = "Creates a new funnel",
            Description = "Creates a new funnel"
        )]
        [SwaggerRequestExample(typeof(Funnel), typeof(FunnelExample))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]        
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] Funnel dto, CancellationToken ct)
        {            
            return Ok(dto);
        }

    }
}
