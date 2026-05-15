namespace Presentation.Common.Swagger;

public sealed class SwaggerConfigureOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "Ledka API",
                Version = description.ApiVersion.ToString(),
                Description = description.IsDeprecated
                    ? "این نسخه منسوخ شده است."
                    : null
            });
        }
    }
}