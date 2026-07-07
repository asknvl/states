using states.Services.LeadEventsService.Application;
using Swashbuckle.AspNetCore.Filters;

namespace states.Dtos.LeadEvents.Examples
{
    public class GetLeadEventsRequestExample : IExamplesProvider<GetLeadEventsRequest>
    {
        public GetLeadEventsRequest GetExamples()
        {
            return new GetLeadEventsRequest(
                TenantId: new Guid("018f3e2a-9c77-7c5b-b2a1-6f4e8d2c9a31"),
                SpaceId: new Guid("018f3e2a-9c77-7c5b-b2a1-6f4e8d2c9a31"),
                LeadId: "018f3e2a-9c77-7c5b-b2a1-6f4e8d2c9a99",
                EventTypes: Enum.GetValues<LeadEventTypes>()
            );
        }
    }
}
