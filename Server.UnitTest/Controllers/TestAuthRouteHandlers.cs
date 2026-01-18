using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using Server.Controllers;
using Server.Models;
using Server.Services.Interfaces;
using Xunit;

namespace Server.UnitTest.Controllers;

public class TestAuthRouteHandlers
{
    const string CstEndSessionEndpoint = "https://logout.example.com/end-session";
    private readonly IOptionsMonitor<OpenIdConnectOptions> _optionsMonitor;

    public TestAuthRouteHandlers()
    {
        var conf = Task.FromResult(new OpenIdConnectConfiguration
        {
            EndSessionEndpoint = CstEndSessionEndpoint
        });
        var oidcOptions = new OpenIdConnectOptions
        {
            ConfigurationManager = Mock.Of<IConfigurationManager<OpenIdConnectConfiguration>>(x =>
                x.GetConfigurationAsync(It.IsAny<CancellationToken>()) == conf
            )
        };

        _optionsMonitor = Mock.Of<IOptionsMonitor<OpenIdConnectOptions>>(x =>
            x.Get(It.IsAny<string>()) == oidcOptions
        );
    }

    [Fact]
    public void GetFullUserInfo_ReturnsUserInfo()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Surname, "DUPONT"),
            new Claim(ClaimTypes.GivenName, "Marcel")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = AuthRouteHandlers.GetFullUserInfo(principal);

        // Assert
        dynamic value = ((dynamic)result).Value;
        var claimsList = ((IEnumerable<dynamic>)value.claims).ToList();
        Assert.Contains(claimsList, x => x.Type == ClaimTypes.Surname && x.Value == "DUPONT");
        Assert.Contains(claimsList, x => x.Type == ClaimTypes.GivenName && x.Value == "Marcel");
    }

    [Fact]
    public async Task GetTokensAsync_ReturnsTokens_WithExpectedTokens()
    {
        // Arrange
        const string accessToken = "accessToken";
        const string idToken = "idToken";
        Task<BffTokensModel> tokensModel = Task.FromResult(new BffTokensModel { AccessToken = accessToken, IdToken = idToken });
        var tokensService = Mock.Of<IBffTokensService>(x =>
            x.GetTokensAsync(OpenIdConnectDefaults.AuthenticationScheme)
            == tokensModel
        );

        // Act
        var result = await AuthRouteHandlers.GetTokensAsync(tokensService);

        // Assert
        var okResult = result as Ok<BffTokensModel>;
        Assert.NotNull(okResult);
        Assert.NotNull(okResult.Value);
        Assert.Equal(accessToken, okResult.Value.AccessToken);
        Assert.Equal(idToken, okResult.Value.IdToken);
    }

    [Fact]
    public async Task GetTokensAsync_ReturnsNull_WhenTokensAreNull()
    {
        // Arrange
        Task<BffTokensModel> tokens = Task.FromResult<BffTokensModel>(null!);
        var tokensService = Mock.Of<IBffTokensService>(x =>
            x.GetTokensAsync(It.IsAny<string>()) == tokens
        );

        // Act
        var result = await AuthRouteHandlers.GetTokensAsync(tokensService);

        // Assert
        var okResult = result as Ok<BffTokensModel>;
        Assert.Null(okResult);
    }

    [Fact]
    public async Task LogoutAsync_ReturnsOk_WithLogoutUrl()
    {
        // Arrange
        const string accessToken = "accessToken";
        const string idToken = "idToken";
        Task<BffTokensModel> tokens = Task.FromResult(new BffTokensModel { AccessToken = accessToken, IdToken = idToken });
        var tokensService = Mock.Of<IBffTokensService>(x =>
            x.GetTokensAsync(OpenIdConnectDefaults.AuthenticationScheme)
            == tokens
        );
        var authService = Mock.Of<IAuthenticationService>();
        var serviceProvider = Mock.Of<IServiceProvider>(x =>
               x.GetService(typeof(IAuthenticationService)) == authService
            && x.GetService(typeof(IOptionsMonitor<OpenIdConnectOptions>)) == _optionsMonitor
        );
        var uri = new Uri("https://somewhere.example.aaa:5050");
        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            Request = { Scheme = uri.Scheme, Host = new HostString(uri.Host, uri.Port), Path = "/api/auth/logout" }
        };

        // Act
        var result = await AuthRouteHandlers.LogoutAsync(context, tokensService);

        // Assert
        var okResult = result as Ok<string>;
        Assert.NotNull(okResult);
        Assert.Equal($"{CstEndSessionEndpoint}?id_token_hint={idToken}&post_logout_redirect_uri={uri}", okResult.Value);
    }

    [Fact]
    public async Task LogoutAsync_WithNullConfigurationManager_ShouldThrow_InvalidOperationException()
    {
        // Arrange
        const string accessToken = "accessToken";
        const string idToken = "idToken";
        Task<BffTokensModel> tokens = Task.FromResult(new BffTokensModel { AccessToken = accessToken, IdToken = idToken });
        var tokensService = Mock.Of<IBffTokensService>(x =>
            x.GetTokensAsync(OpenIdConnectDefaults.AuthenticationScheme)
            == tokens
        );
        var oidcOptions = new OpenIdConnectOptions
        {
            ConfigurationManager = null
        };
        var optionsMonitor = Mock.Of<IOptionsMonitor<OpenIdConnectOptions>>(x =>
            x.Get(It.IsAny<string>()) == oidcOptions
        );
        var authService = Mock.Of<IAuthenticationService>();
        var serviceProvider = Mock.Of<IServiceProvider>(x =>
               x.GetService(typeof(IAuthenticationService)) == authService
            && x.GetService(typeof(IOptionsMonitor<OpenIdConnectOptions>)) == optionsMonitor
        );
        var uri = new Uri("https://somewhere.example.aaa:5050");
        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            Request = { Scheme = uri.Scheme, Host = new HostString(uri.Host, uri.Port), Path = "/api/auth/logout" }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => AuthRouteHandlers.LogoutAsync(context, tokensService));
        Assert.Equal("OIDC Configuration Manager is not available.", ex.Message);
    }
}
