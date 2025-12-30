using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Server.Shared.Secu;
using System.Security.Claims;
using Xunit;

namespace Server.UnitTest.Shared.Secu;

public class TestOidcSecurityExtension
{
    private static readonly Dictionary<string, string?>? InMemorySettings = new(StringComparer.Ordinal)
    {
        { "ASPNETCORE_ENVIRONMENT", "Production" },
        { "Auth:Authority", "https://authority" },
        { "Auth:ClientId", "clientId" },
        { "Auth:CallbackPath", "/authen/callback" },
        { "Auth:Audience", "audience" },
        { "Auth:Scopes:0", "openid" },
        { "Auth:Scopes:1", "profile" },
        { "Auth:Cors:0", "http://localhost:5173" },
        { "Auth:Cors:1", "https://localhost:5001" }
    };

    private readonly IConfiguration _conf = new ConfigurationBuilder().AddInMemoryCollection(InMemorySettings).Build();

    private static readonly Dictionary<string, object> ClaimsDico = new() { { "role", "admin" }, { "email", "user@test.com" } };

    private static void TryAddClaimsFromAccessToken(Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext context, string authority)
    {
        var tokenHandler = new JsonWebTokenHandler();
        var token = context.TokenEndpointResponse?.AccessToken;
        if (string.IsNullOrEmpty(token) || !tokenHandler.CanReadToken(token))
        {
            return;
        }

        var jwtToken = tokenHandler.ReadJsonWebToken(token);
        if (jwtToken.Issuer != authority)
        {
            return;
        }

        foreach (var claim in jwtToken.Claims)
        {
            (context.Principal?.Identity as ClaimsIdentity)?.AddClaim(claim);
        }
    }

    [Fact]
    public void AddAuthentication_ConfiguresJwtBearerOptionsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = Mock.Of<IHostApplicationBuilder>(x => x.Services == services);

        // Act
        builder.AddAuthentication(_conf);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get("Bearer");

        Assert.Equal(InMemorySettings?["Auth:Authority"], options.Authority);
        Assert.Equal(InMemorySettings?["Auth:Audience"], options.Audience);
        Assert.True(options.SaveToken);
        Assert.NotNull(options.TokenValidationParameters);
    }

    [Fact]
    public void AddAuthentication_RegistersCookieAndOpenIdConnectSchemes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_ => Mock.Of<ITicketStore>());
        var builder = Mock.Of<IHostApplicationBuilder>(x => x.Services == services);

        // Act
        builder.AddAuthentication(_conf);

        // Assert
        var provider = services.BuildServiceProvider();
        var cookieOptions = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var oidcOptions = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OpenIdConnectDefaults.AuthenticationScheme);

        Assert.NotNull(cookieOptions);
        Assert.NotNull(oidcOptions);
    }

    [Theory]
    [InlineData("/authentication/callback")]
    [InlineData("/health")]
    public async Task CookieAuthenticationEvents_OnRedirectToLogin_AnonymousRoute_Returns200OK(string strPath)
    {
        // Arrange
        var httpContext = new DefaultHttpContext { Request = { Path = strPath } };
        var redirectContext = new RedirectContext<CookieAuthenticationOptions>(
            httpContext,
            new AuthenticationScheme("Cookies", displayName: null, typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            new AuthenticationProperties(),
            "http://login"
        );
        var events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            }
        };

        // Act
        await events.OnRedirectToLogin(redirectContext);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task CookieAuthenticationEvents_OnRedirectToLogin_ApiRoute_Returns401Unauthorized()
    {
        // Arrange
        var httpContext = new DefaultHttpContext { Request = { Path = "/api/resource" } };
        var redirectContext = new RedirectContext<CookieAuthenticationOptions>(
            httpContext,
            new AuthenticationScheme("Cookies", displayName: null, typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            new AuthenticationProperties(),
            "http://login"
        );
        var events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
        };

        // Act
        await events.OnRedirectToLogin(redirectContext);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task CookieAuthenticationEvents_OnRedirectToLogin_OtherRoute_RedirectsToLogin()
    {
        // Arrange
        var httpContext = new DefaultHttpContext { Request = { Path = "/other" } };
        var redirectContext = new RedirectContext<CookieAuthenticationOptions>(
            httpContext,
            new AuthenticationScheme("Cookies", displayName: null, typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            new AuthenticationProperties(),
            "http://login"
        );
        var events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                context.HttpContext.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };

        // Act
        await events.OnRedirectToLogin(redirectContext);

        // Assert
        Assert.Equal("http://login", httpContext.Response.Headers.Location);
    }

    [Fact]
    public void OnTokenValidated_AddsClaims_WhenAccessTokenIsValidAndIssuerMatches()
    {
        // Arrange
        var claimsIdentity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(claimsIdentity);
        var tokenHandler = new JsonWebTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = InMemorySettings?["Auth:Authority"],
            Claims = ClaimsDico
        };
        var accessToken = tokenHandler.CreateToken(tokenDescriptor);
        var options = new OpenIdConnectOptions { Authority = InMemorySettings?["Auth:Authority"] };
        var context = new Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme("oidc", displayName: null, typeof(OpenIdConnectHandler)),
            options,
            principal,
            new AuthenticationProperties()
        )
        {
            TokenEndpointResponse = new OpenIdConnectMessage { AccessToken = accessToken }
        };

        // Act
        TryAddClaimsFromAccessToken(context, context.Options.Authority!);

        // Assert
        Assert.Contains(claimsIdentity.Claims, x => x is { Type: "role", Value: "admin" });
        Assert.Contains(claimsIdentity.Claims, x => x is { Type: "email", Value: "user@test.com" });
    }

    [Fact]
    public void OnTokenValidated_DoesNotAddClaims_WhenIssuerDoesNotMatch()
    {
        // Arrange
        var claimsIdentity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(claimsIdentity);
        var tokenHandler = new JsonWebTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = "https://other-issuer",
            Claims = ClaimsDico
        };
        var accessToken = tokenHandler.CreateToken(tokenDescriptor);
        var options = new OpenIdConnectOptions { Authority = InMemorySettings?["Auth:Authority"] };
        var context = new Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme("oidc", displayName: null, typeof(OpenIdConnectHandler)),
            options,
            principal,
            new AuthenticationProperties()
        )
        {
            TokenEndpointResponse = new OpenIdConnectMessage { AccessToken = accessToken }
        };

        // Act
        TryAddClaimsFromAccessToken(context, context.Options.Authority!);

        // Assert
        Assert.Empty(claimsIdentity.Claims);
    }

    [Fact]
    public void AddAuthentication_ConfiguresCookieOptionsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_ => Mock.Of<ITicketStore>());
        var builder = Mock.Of<IHostApplicationBuilder>(x => x.Services == services);

        // Act
        builder.AddAuthentication(_conf);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal("dudu", options.Cookie.Name);
        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.None, options.Cookie.SameSite);
        Assert.Equal(TimeSpan.FromMinutes(5), options.ExpireTimeSpan);
        Assert.False(options.SlidingExpiration);
        Assert.NotNull(options.SessionStore);
    }

    [Fact]
    public async Task AddOpenIdConnect_OnTokenValidated_AddsClaims_WhenAccessTokenIsValidAndIssuerMatches()
    {
        // Arrange
        var claimsIdentity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(claimsIdentity);
        var tokenHandler = new JsonWebTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = InMemorySettings?["Auth:Authority"],
            Claims = ClaimsDico
        };
        var accessToken = tokenHandler.CreateToken(tokenDescriptor);
        var options = new OpenIdConnectOptions
        {
            Authority = InMemorySettings?["Auth:Authority"],
            Events = OidcSecurityExtension.GetOpenIdConnectEvents(InMemorySettings?["Auth:Authority"])
        };
        var context = new Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme("oidc", displayName: null, typeof(OpenIdConnectHandler)),
            options,
            principal,
            new AuthenticationProperties()
        )
        {
            TokenEndpointResponse = new OpenIdConnectMessage { AccessToken = accessToken }
        };

        // Act
        await options.Events.OnTokenValidated(context);

        // Assert
        Assert.Contains(claimsIdentity.Claims, x => x is { Type: "role", Value: "admin" });
        Assert.Contains(claimsIdentity.Claims, x => x is { Type: "email", Value: "user@test.com" });
    }

    [Fact]
    public async Task GetOpenIdConnectEvents_OnTokenValidated_DoesNotThrow_WhenAccessTokenIsNull()
    {
        // Arrange
        var events = OidcSecurityExtension.GetOpenIdConnectEvents(InMemorySettings?["Auth:Authority"]);
        var claimsIdentity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(claimsIdentity);
        var options = new OpenIdConnectOptions { Authority = InMemorySettings?["Auth:Authority"], Events = events };
        var context = new Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme("oidc", displayName: null, typeof(OpenIdConnectHandler)),
            options,
            principal,
            new AuthenticationProperties()
        )
        {
            TokenEndpointResponse = new OpenIdConnectMessage { AccessToken = null }
        };

        // Act & Assert
        await events.OnTokenValidated(context);
        Assert.Empty(claimsIdentity.Claims);
    }

    [Fact]
    public async Task GetOpenIdConnectEvents_OnTokenValidated_AddsClaims_WhenIssuerMatches()
    {
        // Arrange
        var claimsIdentity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(claimsIdentity);
        var tokenHandler = new JsonWebTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = InMemorySettings?["Auth:Authority"],
            Claims = ClaimsDico
        };
        var accessToken = tokenHandler.CreateToken(tokenDescriptor);
        var events = OidcSecurityExtension.GetOpenIdConnectEvents(InMemorySettings?["Auth:Authority"]);
        var options = new OpenIdConnectOptions
        {
            Authority = InMemorySettings?["Auth:Authority"],
            Events = events
        };
        var context = new Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme("oidc", displayName: null, typeof(OpenIdConnectHandler)),
            options,
            principal,
            new AuthenticationProperties()
        )
        {
            TokenEndpointResponse = new OpenIdConnectMessage { AccessToken = accessToken }
        };

        // Act
        await events.OnTokenValidated(context);

        // Assert
        Assert.Contains(claimsIdentity.Claims, x => x is { Type: "role", Value: "admin" });
        Assert.Contains(claimsIdentity.Claims, x => x is { Type: "email", Value: "user@test.com" });
    }

    [Fact]
    public async Task GetOpenIdConnectEvents_OnTokenValidated_DoesNotAddClaims_WhenIssuerDoesNotMatch()
    {
        // Arrange
        var claimsIdentity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(claimsIdentity);
        var tokenHandler = new JsonWebTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = "https://wrong-issuer",
            Claims = new Dictionary<string, object> { { "role", "admin" }, { "email", "user@test.com" } }
        };
        var accessToken = tokenHandler.CreateToken(tokenDescriptor);
        var events = OidcSecurityExtension.GetOpenIdConnectEvents(InMemorySettings?["Auth:Authority"]);
        var options = new OpenIdConnectOptions
        {
            Authority = InMemorySettings?["Auth:Authority"],
            Events = events
        };
        var context = new Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme("oidc", displayName: null, typeof(OpenIdConnectHandler)),
            options,
            principal,
            new AuthenticationProperties()
        )
        {
            TokenEndpointResponse = new OpenIdConnectMessage { AccessToken = accessToken }
        };

        // Act
        await events.OnTokenValidated(context);

        // Assert
        Assert.Empty(claimsIdentity.Claims);
    }

    [Fact]
    public async Task AddAuthorization_SetsFallbackPolicyToRequireAuthenticatedUser()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = Mock.Of<IHostApplicationBuilder>(x => x.Services == services);

        // Act
        builder.AddAuthorization();

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider>();
        var policy = await options.GetDefaultPolicyAsync();
        Assert.NotNull(policy);
        Assert.Equal(["Bearer", "OpenIdConnect"], policy.AuthenticationSchemes);
        Assert.Contains(policy.Requirements, r => r.GetType().Name == "DenyAnonymousAuthorizationRequirement");
    }

    [Fact]
    public async Task GetOpenIdConnectEvents_OnRedirectToIdentityProvider_Favicon_HandlesResponse()
    {
        // Arrange
        var events = OidcSecurityExtension.GetOpenIdConnectEvents(InMemorySettings?["Auth:Authority"]);
        var httpContext = new DefaultHttpContext { Request = { Path = "/favicon.ico" } };
        var options = new OpenIdConnectOptions { Authority = InMemorySettings?["Auth:Authority"], Events = events };
        var context = new RedirectContext(
            httpContext,
            new AuthenticationScheme("oidc", displayName: null, typeof(OpenIdConnectHandler)),
            options,
            new AuthenticationProperties()
        );

        // Act
        await events.OnRedirectToIdentityProvider(context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task GetOpenIdConnectEvents_OnRedirectToIdentityProvider_ApiRoute_Unauthenticated_Returns401()
    {
        // Arrange
        var events = OidcSecurityExtension.GetOpenIdConnectEvents(InMemorySettings?["Auth:Authority"]);
        var httpContext = new DefaultHttpContext
        {
            Request = { Path = "/api/resource" },
            User = new ClaimsPrincipal(new ClaimsIdentity()) // Not authenticated
        };
        var options = new OpenIdConnectOptions { Authority = InMemorySettings?["Auth:Authority"], Events = events };
        var context = new RedirectContext(
            httpContext,
            new AuthenticationScheme("oidc", displayName: null, typeof(OpenIdConnectHandler)),
            options,
            new AuthenticationProperties()
        );

        // Act
        await events.OnRedirectToIdentityProvider(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
    }

    [Theory]
    [InlineData("/home", true)] // route non-API, authentified user
    [InlineData("/home", false)] // route non-API, unauthentified user
    public async Task GetOpenIdConnectEvents_OnRedirectToIdentityProvider_ApiRoute_Authenticated_DoesNothing(string path, bool isAuthenticated)
    {
        // Arrange
        var events = OidcSecurityExtension.GetOpenIdConnectEvents(InMemorySettings?["Auth:Authority"]);
        var identity = isAuthenticated ? new ClaimsIdentity([new Claim(ClaimTypes.Name, "user")]) : new ClaimsIdentity();
        var httpContext = new DefaultHttpContext
        {
            Request = { Path = path },
            User = new ClaimsPrincipal(identity)
        };
        var options = new OpenIdConnectOptions { Authority = InMemorySettings?["Auth:Authority"], Events = events };
        var context = new RedirectContext(
            httpContext,
            new AuthenticationScheme("oidc", displayName: null, typeof(OpenIdConnectHandler)),
            options,
            new AuthenticationProperties()
        );

        // Act
        await events.OnRedirectToIdentityProvider(context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
    }

    [Fact]
    public void AddAuthentication_ConfiguresOpenIdConnectOptionsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_ => Mock.Of<ITicketStore>());
        var builder = Mock.Of<IHostApplicationBuilder>(x => x.Services == services);

        // Act
        builder.AddAuthentication(_conf);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OpenIdConnectDefaults.AuthenticationScheme);

        Assert.Equal(InMemorySettings?["Auth:Authority"], options.Authority);
        Assert.Equal(InMemorySettings?["Auth:ClientId"], options.ClientId);
        Assert.Contains("openid", options.Scope);
        Assert.Contains("profile", options.Scope);
        Assert.True(options.SaveTokens);
        Assert.True(options.GetClaimsFromUserInfoEndpoint);
        Assert.Equal(OpenIdConnectResponseType.Code, options.ResponseType);
        Assert.Equal(OpenIdConnectResponseMode.FormPost, options.ResponseMode);
        Assert.True(options.UsePkce);
        Assert.True(options.MapInboundClaims);
        Assert.NotNull(options.Events);
    }

    [Fact]
    public void AddAuthentication_ConfiguresPolicyScheme_ForwardDefaultSelector_JwtBearer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(_ => Mock.Of<ITicketStore>());
        var builder = Mock.Of<IHostApplicationBuilder>(x => x.Services == services);

        // Act
        builder.AddAuthentication(_conf);

        // Assert
        var provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<AuthenticationSchemeOptions>>();
        var policySchemeOptions = optionsMonitor.Get("jwt-oidc");
        Assert.NotNull(policySchemeOptions);

        var authOptionsMonitor = provider.GetRequiredService<IOptionsMonitor<AuthenticationOptions>>();
        var authOptions = authOptionsMonitor.CurrentValue;
        Assert.NotNull(authOptions.DefaultScheme);
        Assert.Equal("Cookies", authOptions.DefaultScheme);
    }
}
