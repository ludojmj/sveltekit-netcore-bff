using Server.Models;

namespace Server.Services.Interfaces;

public interface ITokenService
{
    Task<TokensModel> GetTokensAsync(string oidcScheme);
}
