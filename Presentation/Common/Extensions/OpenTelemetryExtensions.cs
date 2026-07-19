using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Presentation.Common.Diagnostics;

namespace Presentation.Common.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddApplicationObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddSingleton<BusinessMetrics>();

        var samplingRatio = environment.IsProduction() ? 0.1 : 1.0;

        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: "Mechanic.Api",
                serviceVersion: typeof(OpenTelemetryExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                serviceInstanceId: Environment.MachineName)
            .AddAttributes(new KeyValuePair<string, object>[]
            {
                new("deployment.environment", environment.EnvironmentName)
            });

        services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(
                    serviceName: "Mechanic.Api",
                    serviceVersion: typeof(OpenTelemetryExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(new KeyValuePair<string, object>[]
                {
                    new("deployment.environment", environment.EnvironmentName)
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(samplingRatio)))
                    .AddSource(ApplicationActivitySources.AllNames.ToArray())
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = context =>
                        {
                            var path = context.Request.Path.Value ?? string.Empty;
                            return !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                                && !path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase)
                                && !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = !environment.IsProduction();
                    });

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter(BusinessMetrics.MeterName)
                    .AddMeter("Elasticsearch")
                    .AddPrometheusExporter();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                }
            });

        return services;
    }
}
