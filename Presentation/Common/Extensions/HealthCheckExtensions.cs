using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Presentation.Common.Extensions;

public static class HealthCheckExtensions
{
    public const string LiveEndpoint = "/health/live";
    public const string ReadyEndpoint = "/health/ready";
    public const string DetailsEndpoint = "/health/details";

    public const string TagLive = "live";
    public const string TagReady = "ready";
    public const string TagCritical = "critical";

    public static WebApplication MapApplicationHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks(LiveEndpoint, new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteMinimalResponse,
            AllowCachingResponses = false
        }).AllowAnonymous();

        app.MapHealthChecks(ReadyEndpoint, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(TagReady),
            ResponseWriter = WriteMinimalResponse,
            AllowCachingResponses = false
        }).AllowAnonymous();

        app.MapHealthChecks(DetailsEndpoint, new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            AllowCachingResponses = false
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return app;
    }

    private static Task WriteMinimalResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString()
        };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}
