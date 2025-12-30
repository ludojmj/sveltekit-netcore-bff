using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Server.Shared.Secu.Swagger;

public class AddRequiredCsrfHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription?.HttpMethod == null
            || HttpMethods.IsGet(context.ApiDescription.HttpMethod))
        {
            return;
        }

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = Utils.CstAntiforgeryToken,
            In = ParameterLocation.Header,
            Required = false
        });
    }
}
