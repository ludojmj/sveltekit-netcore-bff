using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Server.Shared.Secu;

public class AntiforgeryAndBearerEndpointFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var authenticateResult = await httpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (authenticateResult.Succeeded)
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
