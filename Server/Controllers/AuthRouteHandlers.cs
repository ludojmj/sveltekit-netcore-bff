using System.Security.Claims;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Server.Models;
using Server.Services.Interfaces;
using Server.Shared;

namespace Server.Controllers;

public static class AuthRouteHandlers
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("logout", LogoutAsync)
            .Produces<string>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        group.MapGet("tokens", GetTokensAsync)
            .Produces<BffTokensModel>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        group.MapGet("userinfo", GetUser)
            .Produces<UserModel>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        group.MapGet("fullinfo", GetFullUserInfo)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        group.MapGet("csrf-token", GetCrsf)
            .Produces<string>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        return group;
    }

    internal static async Task<IResult> LogoutAsync(HttpContext ctxUserAuth, IBffTokensService service)
    {
        BffTokensModel result = await service.GetTokensAsync(OpenIdConnectDefaults.AuthenticationScheme);
        await ctxUserAuth.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await ctxUserAuth.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        OpenIdConnectOptions oidcOptions = ctxUserAuth.RequestServices
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OpenIdConnectDefaults.AuthenticationScheme);
        var configurationManager = oidcOptions.ConfigurationManager
            ?? throw new InvalidOperationException("OIDC Configuration Manager is not available.");
        var discoveryDocument = await configurationManager.GetConfigurationAsync(ctxUserAuth.RequestAborted);
        var endSessionEndpoint = discoveryDocument?.EndSessionEndpoint;
        var whereAmI = ctxUserAuth.Request.GetUri().GetLeftPart(UriPartial.Authority);
        var logoutUrl = $"{endSessionEndpoint}?id_token_hint={result.IdToken}&post_logout_redirect_uri={whereAmI}/";
        return Results.Ok(logoutUrl);
    }

    internal static async Task<IResult> GetTokensAsync(IBffTokensService service)
    {
        BffTokensModel result = await service.GetTokensAsync(OpenIdConnectDefaults.AuthenticationScheme);
        return Results.Ok(result);
    }

    internal static IResult GetFullUserInfo(ClaimsPrincipal user)
    {
        var result = new
        {
            name = user.Identity?.Name,
            claims = user.Claims.Select(x => new { x.Type, x.Value })
        };
        return Results.Ok(result);
    }

    internal static IResult GetUser(HttpContext ctxUserAuth)
    {
        UserModel result = ctxUserAuth.GetCurrentUser();
        return Results.Ok(result);
    }

    internal static IResult GetCrsf(HttpContext ctxUserAuth, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(ctxUserAuth);
        string result = tokens.RequestToken;
        return Results.Ok(result);
    }
}
