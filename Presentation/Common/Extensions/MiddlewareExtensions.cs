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

        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.GetLevel = (httpContext, elapsed, ex) =>
                ex != null
                    ? LogEventLevel.Error
                    : httpContext.Response.StatusCode >= 500
                        ? LogEventLevel.Error
                        : httpContext.Response.StatusCode >= 400
                            ? LogEventLevel.Warning
                            : LogEventLevel.Information;

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());

                if (httpContext.User.Identity?.IsAuthenticated == true)
                    diagnosticContext.Set("UserName", httpContext.User.Identity.Name);
            };
        });

        app.UseMiddleware<RequestLoggingEnrichmentMiddleware>();

        app.UseCustomExceptionHandler();

        app.UseMiddleware<SecurityHeadersMiddleware>();

        app.UseRequestPerformanceMonitoring();

        app.UseCustomCors();

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseMiddleware<RateLimitMiddleware>();

        app.UseAdminIpWhitelist();

        app.UseMiddleware<WebhookIpWhitelistMiddleware>();

        return app;
    }
}