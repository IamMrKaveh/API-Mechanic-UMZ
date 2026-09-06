using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Presentation.Common.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Tests.Presentation.Common.Swagger;

public class RemoveVersionParameterOperationFilterTests
{
    private static (OpenApiOperation Operation, OperationFilterContext Context) Build(
        params OpenApiParameter[] parameters)
    {
        var operation = new OpenApiOperation
        {
            Parameters = new List<OpenApiParameter>(parameters)
        };
        var generator = Substitute.For<ISchemaGenerator>();
        var context = new OperationFilterContext(
            new ApiDescription(),
            generator,
            new SchemaRepository(),
            typeof(RemoveVersionParameterOperationFilterTests).GetMethod(
                nameof(Build), BindingFlags.Static | BindingFlags.NonPublic)!);
        return (operation, context);
    }

    [Fact]
    public void Apply_RemovesVersionPathParameter()
    {
        var (operation, context) = Build(
            new OpenApiParameter { Name = "version", In = ParameterLocation.Path },
            new OpenApiParameter { Name = "id", In = ParameterLocation.Path });

        new RemoveVersionParameterOperationFilter().Apply(operation, context);

        operation.Parameters.Count.ShouldBe(1);
        operation.Parameters[0].Name.ShouldBe("id");
    }

    [Fact]
    public void Apply_KeepsVersionQueryParameter()
    {
        var (operation, context) = Build(
            new OpenApiParameter { Name = "version", In = ParameterLocation.Query });

        new RemoveVersionParameterOperationFilter().Apply(operation, context);

        operation.Parameters.Count.ShouldBe(1);
    }

    [Fact]
    public void Apply_WithoutVersionParameter_DoesNothing()
    {
        var (operation, context) = Build(
            new OpenApiParameter { Name = "id", In = ParameterLocation.Path });

        new RemoveVersionParameterOperationFilter().Apply(operation, context);

        operation.Parameters.Count.ShouldBe(1);
        operation.Parameters[0].Name.ShouldBe("id");
    }

    [Fact]
    public void Apply_WithoutParameters_DoesNothing()
    {
        var (operation, context) = Build();

        Should.NotThrow(() => new RemoveVersionParameterOperationFilter().Apply(operation, context));
        operation.Parameters.Count.ShouldBe(0);
    }
}
