using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Moq;
using Server.Shared.Secu;
using System.Security.Claims;
using Xunit;

namespace Server.UnitTest.Shared.Secu;

public class TestLocalMiddlewareExtension
{
    private static async Task InvokeLocalSpaAsync(HttpContext context, IHostEnvironment env)
    {
        var builder = new ApplicationBuilder(serviceProvider: null!);
        builder.UseLocalSpa(env);
        var app = builder.Build();
        await app(context);
    }

    [Fact]
    public async Task InvokeAsync_Redirects_WhenAuthenticatedAndDevelopment()
    {
        // Arrange.
        var env = Mock.Of<IHostEnvironment>(x => x.EnvironmentName == "Development");
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("type", "value")], "TestAuth"));
        var context = new DefaultHttpContext { User = user, Request = { Path = "/port5001/to/port5173", QueryString = new QueryString("?param=value") } };

        // Act
        await InvokeLocalSpaAsync(context, env);

        // Assert
        Assert.Equal("http://localhost:5173/port5001/to/port5173?param=value", context.Response.Headers.Location);
    }

    [Fact]
    public async Task InvokeAsync_CallsNext_WhenAuthenticatedAndServer()
    {
        // Arrange
        var env = Mock.Of<IHostEnvironment>(x => x.EnvironmentName == "Production");
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("type", "value")], "TestAuth"));
        var context = new DefaultHttpContext { User = user, Request = { Path = "/autre/chemin" } };

        // Act
        await InvokeLocalSpaAsync(context, env);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    [Theory]
    [InlineData("/authentication/callback")]
    [InlineData("/ready")]
    [InlineData("/health")]
    public async Task InvokeAsync_CallsNext_WhenAnonymousRouteAndUnauthenticated(string path)
    {
        // Arrange
        var env = Mock.Of<IHostEnvironment>(x => x.EnvironmentName == "Production");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()), Request = { Path = path } };

        // Act
        await InvokeLocalSpaAsync(context, env);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    [Theory]
    [InlineData("/api/test", "Development")]
    [InlineData("/api/test", "Production")]
    [InlineData("/swagger/index.html", "Development")]
    [InlineData("/swagger/index.html", "Production")]
    public async Task InvokeAsync_CallsNext_WhenAuthenticatedAndApiOrSwagger(string path, string targetEnv)
    {
        // Arrange
        var env = Mock.Of<IHostEnvironment>(x => x.EnvironmentName == targetEnv);
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("type", "value")], "TestAuth"));
        var context = new DefaultHttpContext { User = user, Request = { Path = path } };

        // Act
        await InvokeLocalSpaAsync(context, env);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task InvokeAsync_CallsNext_WhenNotAuthenticated(string targetEnv)
    {
        // Arrange
        var env = Mock.Of<IHostEnvironment>(x => x.EnvironmentName == targetEnv);
        var user = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        var context = new DefaultHttpContext { User = user, Request = { Path = "/somepath" } };

        // Act
        await InvokeLocalSpaAsync(context, env);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }
}
