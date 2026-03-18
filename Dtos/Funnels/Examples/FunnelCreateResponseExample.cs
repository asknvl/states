using Swashbuckle.AspNetCore.Filters;

namespace states.Dtos.Funnels.Examples
{
    public class FunnelCreateResponseExample : IMultipleExamplesProvider<FunnelDto>
    {
        public IEnumerable<SwaggerExample<FunnelDto>> GetExamples()
        {
            yield return new SwaggerExample<FunnelDto>
            {
                Name = "FunnelCreated",
                Summary = "Created funnel response",
                Value = new FunnelDto(
                    Id: new Guid("0195930e-84a0-7e12-8d3a-1b5c9f4e7a02"),
                    TenantId: new Guid("0195930e-84a0-7e12-8d3a-1b5c9f4e7a03"),
                    SpaceId: new Guid("0195930e-84a0-7e12-8d3a-1b5c9f4e7a04"),
                    BotId: new Guid("0195930e-84a0-7e12-8d3a-1b5c9f4e7a05"),
                    Name: "Тестовая воронка",
                    Description: "Описание тестовой воронки",
                    Tags: [],
                    Flows: [],
                    IsActive: true
                )
            };
        }
    }
}
