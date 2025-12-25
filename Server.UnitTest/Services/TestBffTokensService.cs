using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Server.Services;
using Xunit;

namespace Server.UnitTest.Services;

public class TestBffTokensService
{
    private static DefaultHttpContext CreateHttpContextWithAuthService(IAuthenticationService authService)
    {
        var services = new ServiceCollection();
        services.AddSingleton(authService);
        var provider = services.BuildServiceProvider();

        return new DefaultHttpContext { RequestServices = provider };
    }

    [Fact]
    public async Task GetTokensAsync_HttpContextIsNull_ThrowsArgumentException()
    {
        // Arrange
        var accessor = Mock.Of<IHttpContextAccessor>(x => x.HttpContext == null);
        var service = new BffTokensService(accessor);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetTokensAsync("oidc"));
    }

    [Fact]
    public async Task GetTokensAsync_AuthenticateFailed_ThrowsArgumentException()
    {
        // Arrange
        var failedResult = AuthenticateResult.Fail("fail");
        Task<AuthenticateResult> result = Task.FromResult(failedResult);
        var authService = Mock.Of<IAuthenticationService>(x =>
            x.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()) == result
        );
        var context = CreateHttpContextWithAuthService(authService);
        var accessor = Mock.Of<IHttpContextAccessor>(x => x.HttpContext == context);
        var service = new BffTokensService(accessor);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetTokensAsync("oidc"));
    }

    [Fact]
    public async Task GetTokensAsync_AuthenticateSuccess_NoAccessToken_ThrowsArgumentException()
    {
        // Arrange
        var successResult = AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(), new AuthenticationProperties(), "oidc")
        );
        Task<AuthenticateResult> result = Task.FromResult(successResult);
        var authService = Mock.Of<IAuthenticationService>(x =>
            x.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()) == result
        );
        var context = CreateHttpContextWithAuthService(authService);
        var accessor = Mock.Of<IHttpContextAccessor>(x => x.HttpContext == context);
        var service = new BffTokensService(accessor);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetTokensAsync("oidc"));
    }

    [Fact]
    public async Task GetTokensAsync_AuthenticateSuccess_WithAccessToken_ReturnsToken()
    {
        // Arrange
        var properties = new AuthenticationProperties();
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = "my_access_token" },
            new AuthenticationToken { Name = "id_token", Value = "my_id_token" }
        ]);
        var successResult = AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(), properties, "oidc")
        );
        Task<AuthenticateResult> result = Task.FromResult(successResult);
        var authService = Mock.Of<IAuthenticationService>(x =>
            x.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()) == result
        );
        var context = CreateHttpContextWithAuthService(authService);
        var accessor = Mock.Of<IHttpContextAccessor>(x => x.HttpContext == context);
        var service = new BffTokensService(accessor);

        // Act
        var tokens = await service.GetTokensAsync("oidc");

        // Assert
        Assert.Equal("my_access_token", tokens.AccessToken);
        Assert.Equal("my_id_token", tokens.IdToken);
    }
}
