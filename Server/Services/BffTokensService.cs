using Microsoft.AspNetCore.Authentication;
using Server.Models;
using Server.Services.Interfaces;

namespace Server.Services;

public class BffTokensService(IHttpContextAccessor ctxUserAuth) : IBffTokensService
{
    private const string CstMsg = "Tokens not found.";

    public async Task<BffTokensModel> GetTokensAsync(string oidcScheme)
    {
        if (ctxUserAuth.HttpContext == null)
        {
            throw new ArgumentException(CstMsg);
        }

        var authenticateResult = await ctxUserAuth.HttpContext.AuthenticateAsync(oidcScheme);
        string accessToken = authenticateResult.Properties?.GetTokenValue("access_token");
        string idToken = authenticateResult.Properties?.GetTokenValue("id_token");
        if (authenticateResult.Succeeded && accessToken != null && idToken != null)
        {
            return new BffTokensModel { AccessToken = accessToken, IdToken = idToken };
        }

        throw new ArgumentException(CstMsg);
    }
}
