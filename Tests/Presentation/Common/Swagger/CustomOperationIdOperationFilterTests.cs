using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Presentation.Common.Swagger;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Tests.Presentation.Common.Swagger;

public class CustomOperationIdOperationFilterTests
{
    private sealed class FakeController : ControllerBase
    {
        [SwaggerOperation(OperationId = "Custom_Op")]
        public void WithAttribute() { }

        public void Plain() { }
    }

    private static OperationFilterContext BuildContext(MethodInfo method, ActionDescriptor? descriptor = null)
    {
        var apiDescription = new ApiDescription
        {
            ActionDescriptor = descriptor ?? new ControllerActionDescriptor
            {
                ControllerName = "Fake",
                ActionName = method.Name,
                MethodInfo = method
            }
        };
        var generator = Substitute.For<ISchemaGenerator>();
        return new OperationFilterContext(
            apiDescription, generator, new SchemaRepository(), method);
    }

    [Fact]
    public void Apply_WhenOperationIdAlreadySet_KeepsIt()
    {
        var operation = new OpenApiOperation { OperationId = "Existing" };
        var context = BuildContext(typeof(FakeController).GetMethod(nameof(FakeController.Plain))!);

        new CustomOperationIdOperationFilter().Apply(operation, context);

        operation.OperationId.ShouldBe("Existing");
    }

    [Fact]
    public void Apply_WithSwaggerOperationAttribute_UsesAttributeOperationId()
    {
        var operation = new OpenApiOperation();
        var context = BuildContext(typeof(FakeController).GetMethod(nameof(FakeController.WithAttribute))!);

        new CustomOperationIdOperationFilter().Apply(operation, context);

        operation.OperationId.ShouldBe("Custom_Op");
    }

    [Fact]
    public void Apply_WithoutAttribute_UsesControllerActionConvention()
    {
        var operation = new OpenApiOperation();
        var method = typeof(FakeController).GetMethod(nameof(FakeController.Plain))!;
        var context = BuildContext(method, new ControllerActionDescriptor
        {
            ControllerName = "OrdersController",
            ActionName = "GetById",
            MethodInfo = method
        });

        new CustomOperationIdOperationFilter().Apply(operation, context);

        operation.OperationId.ShouldBe("Orders_GetById");
    }

    [Fact]
    public void Apply_WithNonControllerDescriptor_LeavesOperationIdEmpty()
    {
        var operation = new OpenApiOperation();
        var apiDescription = new ApiDescription { ActionDescriptor = new ActionDescriptor() };
        var method = typeof(FakeController).GetMethod(nameof(FakeController.Plain))!;
        var context = new OperationFilterContext(
            apiDescription, Substitute.For<ISchemaGenerator>(), new SchemaRepository(), method);

        new CustomOperationIdOperationFilter().Apply(operation, context);

        operation.OperationId.ShouldBeNull();
    }
}
