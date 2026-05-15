namespace Presentation.Common.Swagger;

public sealed class RemoveVersionParameterOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var versionParam = operation.Parameters
            .FirstOrDefault(p => p.Name == "version" && p.In == ParameterLocation.Path);

        if (versionParam is not null)
            operation.Parameters.Remove(versionParam);
    }
}