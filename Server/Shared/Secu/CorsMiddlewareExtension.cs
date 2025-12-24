namespace Server.Shared.Secu;

public static class CorsMiddlewareExtension
{
    public static void UseAuthCors(this IApplicationBuilder app, IConfiguration conf)
    {
        var corsList = conf.GetSection("Auth:Cors").Get<string[]>();
        app.UseCors(corsPolicyBuilder => corsPolicyBuilder
            .WithOrigins(corsList!)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
        );
    }
}
