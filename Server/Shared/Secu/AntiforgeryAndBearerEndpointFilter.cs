using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Server.Shared.Secu;

public class AntiforgeryAndBearerEndpointFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        string authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
        if (httpContext.User.Identity?.IsAuthenticated != true
         || authHeader?.StartsWith(JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal) == true)
        {
            return await next(context);
        }

        var method = httpContext.Request.Method;
        if (HttpMethods.IsPost(method)
         || HttpMethods.IsPut(method)
         || HttpMethods.IsDelete(method)
         || HttpMethods.IsPatch(method))
        {
            await antiforgery.ValidateRequestAsync(httpContext);
        }

        return await next(context);
    }
}
