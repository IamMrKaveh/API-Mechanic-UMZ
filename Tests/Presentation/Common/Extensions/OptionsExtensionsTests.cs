using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Presentation.Common.Extensions;
using Presentation.Common.Options;

namespace Tests.Presentation.Common.Extensions;

public class OptionsExtensionsTests
{
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SecurityHeaders:XFrameOptions"] = "DENY",
                ["Security:AdminIpWhitelist:0"] = "1.2.3.4",
                ["ReviewSettings:RequirePurchaseVerification"] = "true"
            })
            .Build();

    [Fact]
    public void AddPresentationOptions_ConfiguresFormLimits()
    {
        var services = new ServiceCollection();
        services.AddPresentationOptions(BuildConfig());

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<FormOptions>>().Value
            .MultipartBodyLengthLimit.ShouldBe(10 * 1024 * 1024);
    }

    [Fact]
    public void AddPresentationOptions_BindsSecurityHeadersOptions()
    {
        var services = new ServiceCollection();
        services.AddPresentationOptions(BuildConfig());

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<SecurityHeadersOptions>>().Value
            .XFrameOptions.ShouldBe("DENY");
    }

    [Fact]
    public void AddPresentationOptions_BindsSecuritySettings()
    {
        var services = new ServiceCollection();
        services.AddPresentationOptions(BuildConfig());

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<global::Infrastructure.Security.Settings.SecuritySettings>>().Value
            .AdminIpWhitelist.ShouldBe(["1.2.3.4"]);
    }

    [Fact]
    public void AddPresentationOptions_BindsReviewSettings()
    {
        var services = new ServiceCollection();
        services.AddPresentationOptions(BuildConfig());

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<global::Application.Review.Configuration.ReviewSettings>>().Value
            .RequirePurchaseVerification.ShouldBeTrue();
    }

    [Fact]
    public void AddPresentationOptions_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddPresentationOptions(BuildConfig()).ShouldBeSameAs(services);
    }
}
