using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Tides.Api.Startup;

// ASP.NET Core OpenAPI ignores [AllowedValues], so this transformer writes the values as the
// parameter's enum. Generated clients then get the same restriction that validation enforces.
public sealed class AllowedValuesTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var description in context.Description.ParameterDescriptions)
        {
            var allowedValues = (description.ParameterDescriptor as IParameterInfoParameterDescriptor)?
                .ParameterInfo
                .GetCustomAttribute<AllowedValuesAttribute>();
            var parameter = operation.Parameters?.FirstOrDefault(p => p.Name == description.Name);

            if (allowedValues is null || parameter?.Schema is not OpenApiSchema schema)
            {
                continue;
            }

            schema.Enum = [.. allowedValues.Values.Select(value => JsonSerializer.SerializeToNode(value)!)];
        }

        return Task.CompletedTask;
    }
}
