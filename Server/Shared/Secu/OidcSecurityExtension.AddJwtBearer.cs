using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Server.Shared.Secu;

public static partial class OidcSecurityExtension
{
    private static void AddJwtBearer(this AuthenticationBuilder authBuilder, IConfiguration conf) =>
        authBuilder.AddJwtBearer(
            JwtBearerDefaults.AuthenticationScheme,
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                options.Authority = conf["Auth:Authority"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true
                };
            });
}
