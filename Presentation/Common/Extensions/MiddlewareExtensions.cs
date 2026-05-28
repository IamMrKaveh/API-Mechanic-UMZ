using Presentation.Common.Middleware;

namespace Presentation.Common.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseApplication(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseHttpsRedirection();
        app.UseStaticFiles();

        if (app.Configuration.GetValue<bool>("Swagger:Enabled"))
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                var provider = app.Services
                    .GetRequiredService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();

                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        $"Ledka {description.GroupName.ToUpperInvariant()}");
                }

                options.RoutePrefix = "swagger";
            });
        }

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseCustomExceptionHandler();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseRequestPerformanceMonitoring();

        app.UseCustomCors();
        app.UseMiddleware<RateLimitMiddleware>();
        app.UseAdminIpWhitelist();
        app.UseMiddleware<WebhookIpWhitelistMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}