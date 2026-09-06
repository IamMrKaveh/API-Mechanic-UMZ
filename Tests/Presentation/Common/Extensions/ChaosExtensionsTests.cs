using Infrastructure.Chaos.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Presentation.Common.Extensions;

namespace Tests.Presentation.Common.Extensions;

public class ChaosExtensionsTests
{
    private static IConfiguration BuildConfig(bool? enabled = null)
    {
        var data = new List<KeyValuePair<string, string?>>();
        if (enabled.HasValue)
            data.Add(new KeyValuePair<string, string?>("Chaos:IsEnabled", enabled.Value ? "true" : "false"));
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static IHostEnvironment BuildEnvironment(string name)
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(name);
        return env;
    }

    [Fact]
    public void AddChaosEngineering_NonProduction_KeepsConfiguredValue()
    {
        var services = new ServiceCollection();
        services.AddChaosEngineering(BuildConfig(enabled: true), BuildEnvironment(Environments.Development));

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<ChaosOptions>>().Value.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void AddChaosEngineering_Production_ForcesDisabled()
    {
        var services = new ServiceCollection();
        services.AddChaosEngineering(BuildConfig(enabled: true), BuildEnvironment(Environments.Production));

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<ChaosOptions>>().Value.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void AddChaosEngineering_WithoutConfig_DefaultsToDisabled()
    {
        var services = new ServiceCollection();
        services.AddChaosEngineering(BuildConfig(), BuildEnvironment(Environments.Development));

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<ChaosOptions>>().Value.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void AddChaosEngineering_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddChaosEngineering(BuildConfig(), BuildEnvironment(Environments.Development))
            .ShouldBeSameAs(services);
    }
}
