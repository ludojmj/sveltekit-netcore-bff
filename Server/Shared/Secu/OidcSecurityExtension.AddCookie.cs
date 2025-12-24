using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Server.Shared.Secu;

public static partial class OidcSecurityExtension
{
    private static void AddCookie(this AuthenticationBuilder authBuilder) =>
        authBuilder.AddCookie(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CookieAuthenticationDefaults.AuthenticationScheme,
            options =>
            {
                options.Cookie.Name = "dudu";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
                options.SlidingExpiration = false;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(1);
                options.SessionStore = authBuilder.Services.BuildServiceProvider().GetRequiredService<ITicketStore>();
            });
}
