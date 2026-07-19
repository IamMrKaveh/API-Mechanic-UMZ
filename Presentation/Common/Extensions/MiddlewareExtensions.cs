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

        app.UseMiddleware<RequestLoggingMiddleware>();

        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0} ms";

            options.GetLevel = (httpContext, _, ex) =>
                ex is not null || httpContext.Response.StatusCode >= 500
                    ? LogEventLevel.Error
                    : httpContext.Response.StatusCode >= 400
                        ? LogEventLevel.Warning
                        : LogEventLevel.Verbose;

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? "");
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
            };
        });

        app.UseCustomExceptionHandler();

        app.UseMiddleware<SecurityHeadersMiddleware>();

        app.UseRequestPerformanceMonitoring();

        app.UseCustomCors();

        app.UseMiddleware<RateLimitMiddleware>();

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseMiddleware<SessionActivityMiddleware>();

        app.UseAdminIpWhitelist();

        app.UseMiddleware<WebhookIpWhitelistMiddleware>();

        app.MapPrometheusScrapingEndpoint("/metrics").AllowAnonymous();

        app.MapApplicationHealthChecks();

        return app;
    }
}
