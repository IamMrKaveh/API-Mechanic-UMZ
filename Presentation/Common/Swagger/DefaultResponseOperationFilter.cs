namespace Presentation.Common.Swagger;

public sealed class DefaultResponseOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var declaringType = context.MethodInfo.DeclaringType!;

        var hasAllowAnonymous =
            context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() ||
            declaringType.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();

        var hasAuthorize =
            context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ||
            declaringType.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

        EnsureResponse(operation, context, "400", "Bad Request", typeof(ApiResponse));
        EnsureResponse(operation, context, "422", "Unprocessable Entity", typeof(ApiResponse));
        EnsureResponse(operation, context, "500", "Internal Server Error", typeof(ApiResponse));

        if (hasAuthorize && !hasAllowAnonymous)
        {
            EnsureResponse(operation, context, "401", "Unauthorized", typeof(ApiResponse));
            EnsureResponse(operation, context, "403", "Forbidden", typeof(ApiResponse));
        }
    }

    private static void EnsureResponse(
        OpenApiOperation operation,
        OperationFilterContext context,
        string statusCode,
        string description,
        Type type)
    {
        if (operation.Responses.ContainsKey(statusCode))
            return;

        var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);

        operation.Responses[statusCode] = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new() { Schema = schema }
            }
        };
    }
}