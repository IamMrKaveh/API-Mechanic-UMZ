using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Presentation.Base.Responses;
using Presentation.Common.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Tests.Presentation.Common.Swagger;

public class DefaultResponseOperationFilterTests
{
    [AllowAnonymous]
    private sealed class AnonymousController : ControllerBase
    {
        public void Get() { }
    }

    [Authorize]
    private sealed class SecuredController : ControllerBase
    {
        public void Get() { }
    }

    private sealed class MixedController : ControllerBase
    {
        [AllowAnonymous]
        public void Open() { }

        [Authorize]
        public void Closed() { }
    }

    private static OperationFilterContext BuildContext(MethodInfo method)
    {
        var generator = Substitute.For<ISchemaGenerator>();
        generator.GenerateSchema(Arg.Any<Type>(), Arg.Any<SchemaRepository>())
            .Returns(new OpenApiSchema { Type = "object" });
        return new OperationFilterContext(
            new ApiDescription(), generator, new SchemaRepository(), method);
    }

    private static MethodInfo Method<T>(string name) =>
        typeof(T).GetMethod(name, BindingFlags.Public | BindingFlags.Instance)!;

    [Fact]
    public void Apply_AnonymousEndpoint_AddsOnlyCommonErrorResponses()
    {
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };

        new DefaultResponseOperationFilter().Apply(operation, BuildContext(Method<AnonymousController>("Get")));

        operation.Responses.Keys.ShouldBe(["400", "422", "500"], ignoreOrder: true);
        operation.Responses["400"].Description.ShouldBe("Bad Request");
        operation.Responses["422"].Description.ShouldBe("Unprocessable Entity");
        operation.Responses["500"].Description.ShouldBe("Internal Server Error");
        operation.Responses["400"].Content.ShouldContainKey("application/json");
    }

    [Fact]
    public void Apply_AuthorizedEndpoint_Adds401And403()
    {
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };

        new DefaultResponseOperationFilter().Apply(operation, BuildContext(Method<SecuredController>("Get")));

        operation.Responses.Keys.ShouldContain("401");
        operation.Responses.Keys.ShouldContain("403");
        operation.Responses["401"].Description.ShouldBe("Unauthorized");
        operation.Responses["403"].Description.ShouldBe("Forbidden");
    }

    [Fact]
    public void Apply_AuthorizeMethodWithoutClassAuthorize_Adds401And403()
    {
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };

        new DefaultResponseOperationFilter().Apply(operation, BuildContext(Method<MixedController>("Closed")));

        operation.Responses.Keys.ShouldContain("401");
        operation.Responses.Keys.ShouldContain("403");
    }

    [Fact]
    public void Apply_AllowAnonymousMethod_AddsNoAuthResponses()
    {
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };

        new DefaultResponseOperationFilter().Apply(operation, BuildContext(Method<MixedController>("Open")));

        operation.Responses.Keys.ShouldNotContain("401");
        operation.Responses.Keys.ShouldNotContain("403");
        operation.Responses.Keys.ShouldBe(["400", "422", "500"], ignoreOrder: true);
    }

    [Fact]
    public void Apply_ExistingResponseKey_IsNotOverwritten()
    {
        var operation = new OpenApiOperation
        {
            Responses = new OpenApiResponses
            {
                ["400"] = new OpenApiResponse { Description = "Custom validation" }
            }
        };

        new DefaultResponseOperationFilter().Apply(
            operation, BuildContext(Method<AnonymousController>("Get")));

        operation.Responses["400"].Description.ShouldBe("Custom validation");
        operation.Responses.Keys.ShouldBe(["400", "422", "500"], ignoreOrder: true);
    }

    [Fact]
    public void Apply_ResponseSchema_IsGeneratedForApiResponse()
    {
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
        var context = BuildContext(Method<AnonymousController>("Get"));

        new DefaultResponseOperationFilter().Apply(operation, context);

        var schema = operation.Responses["500"].Content["application/json"].Schema;
        schema.ShouldNotBeNull();
        schema.Type.ShouldBe("object");
    }
}
