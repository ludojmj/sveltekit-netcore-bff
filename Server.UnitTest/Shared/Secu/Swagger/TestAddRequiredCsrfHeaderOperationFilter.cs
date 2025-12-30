using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using Server.Shared.Secu.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace Server.UnitTest.Shared.Secu.Swagger;

public class TestAddRequiredCsrfHeaderOperationFilter
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Apply_Parameters_OK(bool isParametersNull)
    {
        // Arrange
        var operation = new OpenApiOperation();
        if (isParametersNull)
        {
            operation.Parameters = null;
        }

        var action = RequestDelegateFactory.Create((string _) => "{}");
        var filterContext = new OperationFilterContext(
            apiDescription: new ApiDescription { HttpMethod = "POST" },
            schemaRegistry: null,
            schemaRepository: null,
            new OpenApiDocument(),
            action.RequestDelegate.Method);

        // Act
        var result = new AddRequiredCsrfHeaderOperationFilter();
        result.Apply(operation, filterContext);

        // Assert
        Assert.NotNull(operation);
        Assert.NotNull(operation.Parameters);
        Assert.Single(operation.Parameters);
        Assert.Equal("RequestVerificationToken", operation.Parameters[0].Name);
        Assert.Equal(ParameterLocation.Header, operation.Parameters[0].In);
        Assert.False(operation.Parameters[0].Required);
    }

    [Fact]
    public void Apply_AddsHeader_WhenGet()
    {
        // Arrange
        var operation = new OpenApiOperation { Parameters = [] };
        var action = RequestDelegateFactory.Create((string _) => "{}");
        var filterContext = new OperationFilterContext(
            apiDescription: new ApiDescription { HttpMethod = "GET" },
            schemaRegistry: null,
            schemaRepository: null,
            new OpenApiDocument(),
            action.RequestDelegate.Method);

        // Act
        var result = new AddRequiredCsrfHeaderOperationFilter();
        result.Apply(operation, filterContext);

        // Assert
        Assert.NotNull(operation.Parameters);
        Assert.Empty(operation.Parameters);
    }

    [Fact]
    public void Apply_Parameters_AddsHeader_WhenApiDescriptionIsNull()
    {
        // Arrange
        var operation = new OpenApiOperation { Parameters = [] };
        var action = RequestDelegateFactory.Create((string _) => "{}");
        var filterContext = new OperationFilterContext(
            apiDescription: null,
            schemaRegistry: null,
            schemaRepository: null,
            new OpenApiDocument(),
            action.RequestDelegate.Method);

        // Act
        var result = new AddRequiredCsrfHeaderOperationFilter();
        result.Apply(operation, filterContext);

        // Assert
        Assert.NotNull(operation.Parameters);
        Assert.Empty(operation.Parameters);
    }
}
