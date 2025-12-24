using Server.Shared;
using Server.Shared.Secu;

namespace Server.Controllers;

public static class Routes
{
    private const string CstStuff = "stuff";
    private const string CstUser = "user";

    public static IEndpointRouteBuilder MapRoutes(this IEndpointRouteBuilder builder)
    {
        var api = builder.MapGroup("api")
            .RequireAuthorization()
            .AddEndpointFilter<AntiforgeryAndBearerEndpointFilter>()
            .AddEndpointFilter<TraceEndpointFilter>();

        api.MapGroup(CstStuff).WithTags(CstStuff).MapStuffEndpoints();
        api.MapGroup(CstUser).WithTags(CstUser).MapUserEndpoints();
        return builder;
    }
}
