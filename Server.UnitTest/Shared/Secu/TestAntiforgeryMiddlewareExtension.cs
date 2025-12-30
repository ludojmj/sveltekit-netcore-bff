using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Moq;
using Server.Shared.Secu;
using Xunit;

namespace Server.UnitTest.Shared.Secu;

public class TestAntiforgeryMiddlewareExtension
{
    private static readonly IAntiforgery Antiforgery = Mock.Of<IAntiforgery>(x => x.GetAndStoreTokens(It.IsAny<HttpContext>()) == new AntiforgeryTokenSet("requestToken", "formToken", "headerName", "cookieToken"));
    private static readonly IServiceProvider ServiceProvider = Mock.Of<IServiceProvider>(x => x.GetService(typeof(IAntiforgery)) == Antiforgery);

    private static async Task InvokeAntiforgeryCookieAsync(HttpContext context)
    {
        var builder = new ApplicationBuilder(ServiceProvider);
        builder.UseStoreAntiforgery();
        var app = builder.Build();
        await app(context);
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetAntiforgeryCookies_WhenGetAndTokenCookieMissing()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            RequestServices = ServiceProvider,
            Request =
            {
                Method = HttpMethods.Get,
                Scheme = "https",
                Host = new HostString("localhost")
            }
        };

        // Act
        await InvokeAntiforgeryCookieAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Set-Cookie"));
        var setCookie = context.Response.Headers.SetCookie.ToString();
        Assert.Equal("RequestVerificationToken=requestToken; path=/; secure; samesite=strict", setCookie);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotSetAntiforgeryCookie_WhenNotGetMethod()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            RequestServices = ServiceProvider,
            Request =
            {
                Method = HttpMethods.Post,
                Scheme = "https",
                Host = new HostString("localhost")
            }
        };

        // Act
        await InvokeAntiforgeryCookieAsync(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Set-Cookie"));
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotSetAntiforgeryCookie_WhenTokenCookieExists()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            RequestServices = ServiceProvider,
            Request =
            {
                Method = HttpMethods.Get,
                Scheme = "https",
                Host = new HostString("localhost"),
                Cookies = Mock.Of<IRequestCookieCollection>(c => c.ContainsKey("RequestVerificationToken"))
            }
        };

        // Act
        await InvokeAntiforgeryCookieAsync(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Set-Cookie"));
    }
}
