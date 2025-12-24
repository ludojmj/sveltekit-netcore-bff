using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Server.Shared.Secu.Swagger;

public class AddRequiredJwtHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "id_token",
            In = ParameterLocation.Header,
            Required = false
        });
    }
}
