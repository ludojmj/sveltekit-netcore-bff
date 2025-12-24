using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Server.Shared.Secu;

public static partial class OidcSecurityExtension
{
    public static void AddAuthentication(this IHostApplicationBuilder builder, IConfiguration conf)
    {
        var authBuilder = builder.AddAuthenticationCore();
        authBuilder.AddJwtBearer(conf);
        authBuilder.AddCookie();
        authBuilder.AddOpenIdConnect(conf);
    }

    public static void AddAuthorization(this IHostApplicationBuilder builder)
    {
        builder.Services.AddAuthorization(options =>
        {
            var schemeTab = new[]
            {
                JwtBearerDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme
            };
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(schemeTab)
                .RequireAuthenticatedUser()
                .Build();

            options.FallbackPolicy = options.DefaultPolicy;
        });
    }
}
