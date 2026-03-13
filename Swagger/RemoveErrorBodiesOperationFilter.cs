using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace states.Swagger
{
    public sealed class RemoveErrorBodiesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            foreach (var (statusCode, response) in operation.Responses)
            {
                if (statusCode.StartsWith("4") || statusCode.StartsWith("5"))
                {
                    response.Content?.Clear();
                }
            }
        }
    }
}