using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Server.Shared.Secu;

public static partial class OidcSecurityExtension
{
    private static void AddOpenIdConnect(this AuthenticationBuilder authBuilder, IConfiguration conf) =>
        authBuilder.AddOpenIdConnect(
            OpenIdConnectDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme,
            options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = conf["Auth:Authority"];
                options.ClientId = conf["Auth:ClientId"];
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.ResponseMode = OpenIdConnectResponseMode.FormPost;
                options.CallbackPath = new PathString(conf["Auth:CallbackPath"]);
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.TokenValidationParameters.NameClaimType = "name";
                options.UsePkce = true;
                options.MapInboundClaims = true;
                options.Events = GetOpenIdConnectEvents(conf["Auth:Authority"]);
                foreach (var scope in conf.GetSection("Auth:Scopes").Get<string[]>())
                {
                    options.Scope.Add(scope);
                }
            });

    internal static OpenIdConnectEvents GetOpenIdConnectEvents(string issuer) => new()
    {
        OnTokenValidated = ctx =>
        {
            var tokenHandler = new JsonWebTokenHandler();
            var authToken = ctx.TokenEndpointResponse?.AccessToken;
            if (!tokenHandler.CanReadToken(authToken))
            {
                return Task.CompletedTask;
            }

            var jwtToken = tokenHandler.ReadJsonWebToken(authToken);
            if (jwtToken.Issuer != issuer)
            {
                return Task.CompletedTask;
            }

            foreach (var claim in jwtToken.Claims)
            {
                (ctx.Principal?.Identity as ClaimsIdentity)?.AddClaim(claim);
            }

            return Task.CompletedTask;
        },
        OnRedirectToIdentityProvider = ctx =>
        {
            var path = ctx.Request.Path;
            if (Utils.CstAnonymousRoutes.Any(x => path.StartsWithSegments(x)))
            {
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                ctx.HandleResponse();
                return Task.CompletedTask;
            }

            if (path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.HandleResponse();
            }

            return Task.CompletedTask;
        }
    };
}
