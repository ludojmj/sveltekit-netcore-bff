using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Server.Shared.Secu;
using System.Security.Claims;
using Xunit;

namespace Server.UnitTest.Shared.Secu;

public class TestAntiforgeryAndBearerEndpointFilter
{
    private readonly IAntiforgery _antiforgery = Mock.Of<IAntiforgery>();
    private readonly EndpointFilterDelegate _nextMock = Mock.Of<EndpointFilterDelegate>();
    private static readonly AntiforgeryTokenSet CookList = new("requestToken", "formToken", "headerName", "cookieToken");

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var context = CreateHttpContext("GET");
        context.User = new ClaimsPrincipal(new ClaimsIdentity());

        var executingContext = Mock.Of<EndpointFilterInvocationContext>(x => x.HttpContext == context);
        var filter = new AntiforgeryAndBearerEndpointFilter(_antiforgery);

        // Act
        await filter.InvokeAsync(executingContext, _nextMock);

        // Assert
        Mock.Get(_nextMock).Verify(x => x(executingContext), Times.Once);
        Mock.Get(_antiforgery).Verify(x => x.ValidateRequestAsync(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenGetAndBearerAuthenticationFails()
    {
        // Arrange
        var context = CreateHttpContext("GET", AuthenticateResult.Fail("Invalid bearer token"));
        context.User = new ClaimsPrincipal(new ClaimsIdentity());
        context.Request.Headers.Authorization = "Bearer token";

        var executingContext = Mock.Of<EndpointFilterInvocationContext>(x => x.HttpContext == context);
        var filter = new AntiforgeryAndBearerEndpointFilter(_antiforgery);

        // Act
        await filter.InvokeAsync(executingContext, _nextMock);

        // Assert
        Mock.Get(_nextMock).Verify(x => x(executingContext), Times.Once);
        Mock.Get(_antiforgery).Verify(x => x.ValidateRequestAsync(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotValidateRequestAsync_WhenPostAndBearerAuthenticationSucceeds()
    {
        // Arrange
        var antiforgery = Mock.Of<IAntiforgery>(x =>
            x.GetAndStoreTokens(It.IsAny<HttpContext>()) == CookList
         && x.ValidateRequestAsync(It.IsAny<HttpContext>()) == Task.CompletedTask
        );
        var context = CreateHttpContext("POST", BearerAuthenticateSuccess());
        context.Request.Cookies = Mock.Of<IRequestCookieCollection>(x => x["XSRF-TOKEN"] == "token");
        context.Request.Headers.Authorization = "Bearer token";

        var executingContext = Mock.Of<EndpointFilterInvocationContext>(x => x.HttpContext == context);
        var filter = new AntiforgeryAndBearerEndpointFilter(antiforgery);

        // Act
        await filter.InvokeAsync(executingContext, _nextMock);

        // Assert
        Mock.Get(_nextMock).Verify(x => x(executingContext), Times.Once);
        Mock.Get(antiforgery).Verify(x => x.ValidateRequestAsync(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotValidateRequestAsync_When_Get()
    {
        // Arrange
        var context = CreateHttpContext("GET");
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("type", "value")], "auth"));
        context.Request.Cookies = Mock.Of<IRequestCookieCollection>(x => x["XSRF-TOKEN"] == "token");

        var executingContext = Mock.Of<EndpointFilterInvocationContext>(x => x.HttpContext == context);
        var filter = new AntiforgeryAndBearerEndpointFilter(_antiforgery);

        // Act
        await filter.InvokeAsync(executingContext, _nextMock);

        // Assert
        Mock.Get(_nextMock).Verify(x => x(executingContext), Times.Once);
        Mock.Get(_antiforgery).Verify(x => x.ValidateRequestAsync(context), Times.Never);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public async Task InvokeAsync_ShouldValidateRequestAsync_WhenMethodIsStateChanging_AndUserIsNotAuthenticated(string method)
    {
        // Arrange
        var antiforgery = Mock.Of<IAntiforgery>(x =>
            x.ValidateRequestAsync(It.IsAny<HttpContext>()) == Task.CompletedTask
        );
        var nextMock = Mock.Of<EndpointFilterDelegate>();
        var context = CreateHttpContext(method);
        context.User = new ClaimsPrincipal(new ClaimsIdentity());

        var executingContext = Mock.Of<EndpointFilterInvocationContext>(x => x.HttpContext == context);
        var filter = new AntiforgeryAndBearerEndpointFilter(antiforgery);

        // Act
        await filter.InvokeAsync(executingContext, nextMock);

        // Assert
        Mock.Get(antiforgery).Verify(x => x.ValidateRequestAsync(context), Times.Once);
        Mock.Get(nextMock).Verify(x => x(executingContext), Times.Once);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public async Task InvokeAsync_ShouldValidateRequestAsync_WhenMethodIsStateChanging_AndBearerAuthenticationFails(string method)
    {
        // Arrange
        var antiforgery = Mock.Of<IAntiforgery>(x =>
            x.ValidateRequestAsync(It.IsAny<HttpContext>()) == Task.CompletedTask
        );
        var nextMock = Mock.Of<EndpointFilterDelegate>();
        var context = CreateHttpContext(method, AuthenticateResult.Fail("Invalid bearer token"));
        context.Request.Headers.Authorization = "Bearer token";

        var executingContext = Mock.Of<EndpointFilterInvocationContext>(x => x.HttpContext == context);
        var filter = new AntiforgeryAndBearerEndpointFilter(antiforgery);

        // Act
        await filter.InvokeAsync(executingContext, nextMock);

        // Assert
        Mock.Get(antiforgery).Verify(x => x.ValidateRequestAsync(context), Times.Once);
        Mock.Get(nextMock).Verify(x => x(executingContext), Times.Once);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public async Task InvokeAsync_ShouldNotValidateRequestAsync_WhenMethodIsStateChanging_AndBearerAuthenticationSucceeds(string method)
    {
        // Arrange
        var antiforgery = Mock.Of<IAntiforgery>(x =>
            x.ValidateRequestAsync(It.IsAny<HttpContext>()) == Task.CompletedTask
        );
        var nextMock = Mock.Of<EndpointFilterDelegate>();
        var context = CreateHttpContext(method, BearerAuthenticateSuccess());
        context.Request.Headers.Authorization = "Bearer token";

        var executingContext = Mock.Of<EndpointFilterInvocationContext>(x => x.HttpContext == context);
        var filter = new AntiforgeryAndBearerEndpointFilter(antiforgery);

        // Act
        await filter.InvokeAsync(executingContext, nextMock);

        // Assert
        Mock.Get(antiforgery).Verify(x => x.ValidateRequestAsync(context), Times.Never);
        Mock.Get(nextMock).Verify(x => x(executingContext), Times.Once);
    }

    private static DefaultHttpContext CreateHttpContext(string method, AuthenticateResult? authenticateResult = null)
    {
        var authenticationService = Mock.Of<IAuthenticationService>(x =>
            x.AuthenticateAsync(It.IsAny<HttpContext>(), JwtBearerDefaults.AuthenticationScheme)
            == Task.FromResult(authenticateResult ?? AuthenticateResult.NoResult())
        );

        return new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton(authenticationService)
                .BuildServiceProvider(),
            Request = { Method = method, Path = "/path" }
        };
    }

    private static AuthenticateResult BearerAuthenticateSuccess()
    {
        var claims = new[] { new Claim("type", "value") };
        var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, JwtBearerDefaults.AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }
}
