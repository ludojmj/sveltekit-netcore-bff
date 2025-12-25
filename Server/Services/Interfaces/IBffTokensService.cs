using Server.Models;

namespace Server.Services.Interfaces;

public interface IBffTokensService
{
    Task<BffTokensModel> GetTokensAsync(string oidcScheme);
}
