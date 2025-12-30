namespace Server.Shared.Secu;

public static class LocalMiddlewareExtension
{
    public static void UseLocalSpa(this IApplicationBuilder app, IHostEnvironment env)
    {
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
            var isNotApiOrSwaggerOrAnonymous =
                !path.StartsWithSegments("/api") &&
                !path.StartsWithSegments("/swagger") &&
                !Utils.CstAnonymousRoutes.Any(path.StartsWithSegments);

            if (env.IsDevelopment() && isAuthenticated && isNotApiOrSwaggerOrAnonymous)
            {
                var redirectUrl = $"http://localhost:5173{context.Request.Path}{context.Request.QueryString}";
                context.Response.Redirect(redirectUrl);
                return;
            }

            await next(context);
        });
    }
}
