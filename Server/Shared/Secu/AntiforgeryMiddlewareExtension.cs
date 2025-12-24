using Microsoft.AspNetCore.Antiforgery;

namespace Server.Shared.Secu;

public static class AntiforgeryMiddlewareExtension
{
    public static void UseStoreAntiforgery(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();

            if (!HttpMethods.IsGet(context.Request.Method))
            {
                await next(context);
                return;
            }

            var cookieName = Utils.CstAntiforgeryToken;
            if (!context.Request.Cookies.ContainsKey(cookieName))
            {
                var tokens = antiforgery.GetAndStoreTokens(context);
                context.Response.Cookies.Append(
                    cookieName,
                    tokens.RequestToken ?? string.Empty,
                    new CookieOptions
                    {
                        HttpOnly = false,
                        SameSite = SameSiteMode.Strict,
                        Secure = context.Request.IsHttps
                    });
            }

            await next(context);
        });
    }
}
