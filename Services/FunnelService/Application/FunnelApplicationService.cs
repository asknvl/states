using DTO = states.Dtos.Funnels;

namespace states.Services.FunnelService.Application
{
    public class FunnelApplicationService : IFunnelsApplicationService
    {        
        public async Task<DTO.Funnel> Create(DTO.Funnel dto)
        {
            await Task.CompletedTask;
            return dto;
        }

        public async Task<DTO.Funnel> Update(DTO.Funnel dto)
        {
            await Task.CompletedTask;
            return dto;
        }

    }
}
