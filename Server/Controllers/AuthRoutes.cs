using Server.Shared;
using Server.Shared.Secu;

namespace Server.Controllers;

public static class AuthRoutes
{
    private const string CstAuth = "auth";

    public static IEndpointRouteBuilder MapAuthRoutes(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/favicon.ico", Results.NoContent).AllowAnonymous().ExcludeFromDescription();

        var api = builder.MapGroup("api")
            .RequireAuthorization()
            .AddEndpointFilter<AntiforgeryAndBearerEndpointFilter>()
            .AddEndpointFilter<TraceEndpointFilter>();

        api.MapGroup(CstAuth).WithTags(CstAuth).MapAuthEndpoints();
        return builder;
    }
}
