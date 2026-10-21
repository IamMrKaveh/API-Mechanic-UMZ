using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Presentation.Common.Extensions;
using SharedContracts.Diagnostics;

namespace Tests.Presentation.Common.Extensions;

public class OpenTelemetryExtensionsTests
{
    private static IHostEnvironment BuildEnvironment(string name)
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(name);
        return env;
    }

    [Fact]
    public void AddApplicationObservability_DoesNotRegisterBusinessMetrics()
    {
        // BusinessMetrics is owned by the Infrastructure layer, which registers the
        // services that emit the metrics. Presentation only wires it to the meter.
        var services = new ServiceCollection();
        services.AddApplicationObservability(
            new ConfigurationBuilder().Build(),
            BuildEnvironment(Environments.Development));

        services.ShouldNotContain(s => s.ServiceType == typeof(BusinessMetrics));
    }

    [Fact]
    public void AddApplicationObservability_RegistersTracerAndMeterProviders()
    {
        var services = new ServiceCollection();
        services.AddApplicationObservability(
            new ConfigurationBuilder().Build(),
            BuildEnvironment(Environments.Development));

        services.ShouldContain(s => s.ServiceType == typeof(TracerProvider));
        services.ShouldContain(s => s.ServiceType == typeof(MeterProvider));
    }

    [Fact]
    public void AddApplicationObservability_WithOtlpEndpoint_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenTelemetry:OtlpEndpoint"] = "http://localhost:4317"
            })
            .Build();
        var services = new ServiceCollection();

        Should.NotThrow(() => services.AddApplicationObservability(config, BuildEnvironment(Environments.Production)));
    }

    [Fact]
    public void AddApplicationObservability_WithoutOtlpEndpoint_DoesNotThrow()
    {
        var services = new ServiceCollection();

        Should.NotThrow(() => services.AddApplicationObservability(
            new ConfigurationBuilder().Build(), BuildEnvironment(Environments.Production)));
    }

    [Fact]
    public void AddApplicationObservability_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddApplicationObservability(
                new ConfigurationBuilder().Build(),
                BuildEnvironment(Environments.Development))
            .ShouldBeSameAs(services);
    }
}
