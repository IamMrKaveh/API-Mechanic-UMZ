using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Presentation.Common.Extensions;
using SharedContracts.FeatureManagement;

namespace Tests.Presentation.Common.Extensions;

public class FeatureManagementExtensionsTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, string?> AllFlags(bool value = true) =>
        FeatureFlags.All.ToDictionary(f => $"FeatureManagement:{f}", f => (string?)(value ? "true" : "false"));

    [Fact]
    public async Task AddFeatureFlags_RegistersFeatureManagerBoundToSection()
    {
        var services = new ServiceCollection();
        services.AddFeatureFlags(BuildConfig(AllFlags()));

        var manager = services.BuildServiceProvider().GetRequiredService<IFeatureManager>();

        (await manager.IsEnabledAsync(FeatureFlags.PaymentCallbackSignatureRequired)).ShouldBeTrue();
        (await manager.IsEnabledAsync(FeatureFlags.StoragePresignedUrlEnabled)).ShouldBeTrue();
    }

    [Fact]
    public void AddFeatureFlags_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddFeatureFlags(BuildConfig(AllFlags())).ShouldBeSameAs(services);
    }

    [Fact]
    public void ValidateFeatureFlagsPresence_WhenSectionMissing_Throws()
    {
        var config = new ConfigurationBuilder().Build();

        var ex = Should.Throw<InvalidOperationException>(() =>
            config.ValidateFeatureFlagsPresence());

        ex.Message.ShouldContain("FeatureManagement");
    }

    [Fact]
    public void ValidateFeatureFlagsPresence_WhenFlagMissing_ThrowsListingIt()
    {
        var values = AllFlags();
        values.Remove($"FeatureManagement:{FeatureFlags.SagaAutoRefundOnCommitFailure}");
        var config = BuildConfig(values);

        var ex = Should.Throw<InvalidOperationException>(() =>
            config.ValidateFeatureFlagsPresence());

        ex.Message.ShouldContain(FeatureFlags.SagaAutoRefundOnCommitFailure);
    }

    [Fact]
    public void ValidateFeatureFlagsPresence_WhenAllPresent_Passes()
    {
        Should.NotThrow(() => BuildConfig(AllFlags()).ValidateFeatureFlagsPresence());
        Should.NotThrow(() => BuildConfig(AllFlags(false)).ValidateFeatureFlagsPresence());
    }
}
