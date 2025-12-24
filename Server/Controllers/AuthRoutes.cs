using Server.Shared;
using Server.Shared.Secu;

namespace Server.Controllers;

public static class AuthRoutes
{
    private const string CstAuth = "auth";

    public static IEndpointRouteBuilder MapAuthRoutes(this IEndpointRouteBuilder group)
    {
        group.MapGet("/favicon.ico", Results.NoContent).AllowAnonymous();

        var api = group.MapGroup("api")
            .RequireAuthorization()
            .AddEndpointFilter<AntiforgeryAndBearerEndpointFilter>()
            .AddEndpointFilter<TraceEndpointFilter>();

        api.MapGroup(CstAuth).WithTags(CstAuth).MapAuthEndpoints();
        return group;
    }
}
