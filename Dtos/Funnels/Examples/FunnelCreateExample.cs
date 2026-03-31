using Swashbuckle.AspNetCore.Filters;

namespace states.Dtos.Funnels.Examples
{
    public class FunnelCreateExample : IMultipleExamplesProvider<FunnelDto>
    {
        public IEnumerable<SwaggerExample<FunnelDto>> GetExamples()
        {
            yield return new SwaggerExample<FunnelDto>
            {
                Name = "Funnel example",
                Summary = "Funnel example",
                Value = new FunnelDto(
                    Id: new Guid("0195930e-84a0-7e12-8d3a-1b5c9f4e7a02"),
                    TenantId: new Guid("018f3e2a-9c77-7c5b-b2a1-6f4e8d2c9a31"),
                    SpaceId: new Guid("018f3e2a-9c77-7c5b-b2a1-6f4e8d2c9a31"),                    
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
