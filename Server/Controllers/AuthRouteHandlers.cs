using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services.Interfaces;
using Server.Shared;

namespace Server.Controllers;

public static class AuthRouteHandlers
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("logout", LogoutAsync)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        group.MapGet("tokens", GetTokensAsync)
            .Produces<TokensModel>()
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

    internal static async Task<IResult> LogoutAsync(HttpContext ctxUserAuth, ITokenService serviceToken)
    {
        TokensModel result = await serviceToken.GetTokensAsync(OpenIdConnectDefaults.AuthenticationScheme);
        await ctxUserAuth.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok(result.IdToken);
    }

    internal static async Task<IResult> GetTokensAsync(ITokenService serviceToken)
    {
        TokensModel result = await serviceToken.GetTokensAsync(OpenIdConnectDefaults.AuthenticationScheme);
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
