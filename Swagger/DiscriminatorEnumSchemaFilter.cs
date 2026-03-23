using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Nodes;

namespace states.Swagger
{
    public class DiscriminatorEnumSchemaFilter : ISchemaFilter
    {
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Discriminator is null || schema.Discriminator.Mapping.Count == 0)
                return;

            var propName = schema.Discriminator.PropertyName;
            if (!schema.Properties.TryGetValue(propName, out var prop))
                return;

            if (prop is not OpenApiSchema concreteProp)
                return;

            concreteProp.Enum ??= new List<JsonNode>();
            foreach (var v in schema.Discriminator.Mapping.Keys)
                concreteProp.Enum.Add(JsonValue.Create(v)!);
        }
    }
}
