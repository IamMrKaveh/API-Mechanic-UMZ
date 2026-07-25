namespace Presentation.Common.Swagger;

public sealed class CustomOperationIdOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.IsNullOrWhiteSpace(operation.OperationId))
            return;

        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor descriptor)
            return;

        var swaggerOp = descriptor.MethodInfo.GetCustomAttribute<Swashbuckle.AspNetCore.Annotations.SwaggerOperationAttribute>();
        if (swaggerOp is not null && !string.IsNullOrWhiteSpace(swaggerOp.OperationId))
        {
            operation.OperationId = swaggerOp.OperationId;
            return;
        }

        var controller = descriptor.ControllerName.Replace("Controller", string.Empty, StringComparison.OrdinalIgnoreCase);
        var action = descriptor.ActionName;
        operation.OperationId = $"{controller}_{action}";
    }
}
