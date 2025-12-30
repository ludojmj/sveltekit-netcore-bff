using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Moq;
using Server.Shared.Secu;
using System.Security.Claims;
using Xunit;

namespace Server.UnitTest.Shared.Secu;

public class TestAntiforgeryAndBearerEndpointFilter
{
    private readonly IAntiforgery _antiforgery = Mock.Of<IAntiforgery>();
    private readonly EndpointFilterDelegate _nextMock = Mock.Of<EndpointFilterDelegate>();
    private static readonly ClaimsPrincipal Identite = new(new ClaimsIdentity([new Claim("type", "value")], "auth"));
    private static readonly AntiforgeryTokenSet CookList = new("requestToken", "formToken", "headerName", "cookieToken");

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var context = Mock.Of<HttpContext>(x =>
            x.User == new ClaimsPrincipal(new ClaimsIdentity())
         && x.Request.Path == "/chemin"
         && x.Request.Method == "GET"
         && x.Request.Headers == new HeaderDictionary()
        );
        var executingContext = Mock.Of<EndpointFilterInvocationContext>(x => x.HttpContext == context);
        var filter = new AntiforgeryAndBearerEndpointFilter(_antiforgery);

        // Act
        await filter.InvokeAsync(executingContext, _nextMock);

        // Assert
        Mock.Get(_nextMock).Verify(x => x(executingContext), Times.Once);
        Mock.Get(_antiforgery).Verify(x => x.ValidateRequestAsync(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenUserIsExternalConsumer()
    {
        // Arrange
        var context = Mock.Of<HttpContext>(x =>
            x.User == new ClaimsPrincipal(new ClaimsIdentity())
         && x.Request.Path == "/chemin"
         && x.Request.Method == "GET"
         && x.Request.Headers == new HeaderDictionary { { "Authorization", "Bearer token" } }
        );
        var executingContext = Mock.Of<EndpointFilterInvocationContext>(x => x.HttpContext == context);
        var filter = new AntiforgeryAndBearerEndpointFilter(_antiforgery);

        // Act
        await filter.InvokeAsync(executingContext, _nextMock);

        // Assert
        Mock.Get(_nextMock).Verify(x => x(executingContext), Times.Once);
        Mock.Get(_antiforgery).Verify(x => x.ValidateRequestAsync(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldValidateRequestAsync_WhenPostAndAuthenticated()
    {
        // Arrange
        Task done = Task.CompletedTask;
        var antiforgery = Mock.Of<IAntiforgery>(x =>
            x.GetAndStoreTokens(It.IsAny<HttpContext>()) == CookList
         && x.ValidateRequestAsync(It.IsAny<HttpContext>()) == done
        );
        var context = Mock.Of<HttpContext>(x =>
            x.User == Identite
         && x.Request.Path == "/chemin"
         && x.Request.Method == "POST"
         && x.Request.Cookies["RequestVerificationToken"] == "token"
         && x.Request.Headers == new HeaderDictionary()
        );
        var executingContext = Mock.Of<EndpointFilterInvocationContext>(x => x.HttpContext == context);

        var filter = new AntiforgeryAndBearerEndpointFilter(antiforgery);

        // Act
        await filter.InvokeAsync(executingContext, _nextMock);

        // Assert
        Mock.Get(_nextMock).Verify(x => x(executingContext), Times.Once);
        Mock.Get(antiforgery).Verify(x => x.ValidateRequestAsync(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotValidateRequestAsync_When_Get()
    {
        // Arrange
        var context = Mock.Of<HttpContext>(x =>
            x.User == Identite
         && x.Request.Path == "/chemin"
         && x.Request.Method == "GET"
         && x.Request.Cookies["RequestVerificationToken"] == "token"
         && x.Request.Headers == new HeaderDictionary()
        );
        var executingContext = Mock.Of<EndpointFilterInvocationContext>(x => x.HttpContext == context);
        var filter = new AntiforgeryAndBearerEndpointFilter(_antiforgery);

        // Act
        await filter.InvokeAsync(executingContext, _nextMock);

        // Assert
        Mock.Get(_nextMock).Verify(x => x(executingContext), Times.Once);
        Mock.Get(_antiforgery).Verify(x => x.ValidateRequestAsync(context), Times.Never);
    }
}
