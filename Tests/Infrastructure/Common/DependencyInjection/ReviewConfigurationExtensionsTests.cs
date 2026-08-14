using Application.Review.Configuration;
using Infrastructure.Common.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Tests.Infrastructure.Common.DependencyInjection;

public class ReviewConfigurationExtensionsTests
{
    [Fact]
    public void AddReviewSettings_ReturnsSameServiceCollectionInstance_ForFluentChaining()
    {
        var services = new ServiceCollection(); var configuration = BuildConfiguration(new Dictionary<string, string?>());

        var result = services.AddReviewSettings(configuration);

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddReviewSettings_WithEmptyConfiguration_ResolvesReviewSettingsWithClassDefaults()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        services.AddReviewSettings(configuration);
        var provider = services.BuildServiceProvider();

        var settings = provider.GetRequiredService<IOptions<ReviewSettings>>().Value;

        settings.PurchaseReviewWindowDays.ShouldBe(90);
        settings.MinCommentLength.ShouldBe(10);
        settings.MaxCommentLength.ShouldBe(1000);
        settings.MaxTitleLength.ShouldBe(100);
        settings.MaxAdminReplyLength.ShouldBe(1000);
        settings.MaxRejectionReasonLength.ShouldBe(500);
        settings.RequirePurchaseVerification.ShouldBeFalse();
        settings.EnableLikeDislike.ShouldBeFalse();
        settings.RateLimit.ShouldNotBeNull();
    }

    [Fact]
    public void AddReviewSettings_WithValidConfiguration_BindsProvidedValues()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ReviewSettings:RequirePurchaseVerification"] = "true",
            ["ReviewSettings:PurchaseReviewWindowDays"] = "30",
            ["ReviewSettings:MinCommentLength"] = "5",
            ["ReviewSettings:MaxCommentLength"] = "800",
            ["ReviewSettings:MaxTitleLength"] = "120",
            ["ReviewSettings:MaxAdminReplyLength"] = "1500",
            ["ReviewSettings:MaxRejectionReasonLength"] = "250",
            ["ReviewSettings:EnableLikeDislike"] = "true",
            ["ReviewSettings:RateLimit:CreateReviewPerMinute"] = "8",
            ["ReviewSettings:RateLimit:PublicReadsPerMinute"] = "120",
            ["ReviewSettings:RateLimit:AdminActionsPerMinute"] = "40",
            ["ReviewSettings:RateLimit:VotePerMinute"] = "25"
        });
        services.AddReviewSettings(configuration);
        var provider = services.BuildServiceProvider();

        var settings = provider.GetRequiredService<IOptions<ReviewSettings>>().Value;

        settings.RequirePurchaseVerification.ShouldBeTrue();
        settings.PurchaseReviewWindowDays.ShouldBe(30);
        settings.MinCommentLength.ShouldBe(5);
        settings.MaxCommentLength.ShouldBe(800);
        settings.MaxTitleLength.ShouldBe(120);
        settings.MaxAdminReplyLength.ShouldBe(1500);
        settings.MaxRejectionReasonLength.ShouldBe(250);
        settings.EnableLikeDislike.ShouldBeTrue();
        settings.RateLimit.CreateReviewPerMinute.ShouldBe(8);
        settings.RateLimit.PublicReadsPerMinute.ShouldBe(120);
        settings.RateLimit.AdminActionsPerMinute.ShouldBe(40);
        settings.RateLimit.VotePerMinute.ShouldBe(25);
    }

    [Fact]
    public void AddReviewSettings_WhenMinCommentLengthExceedsMaxCommentLength_ThrowsOptionsValidationExceptionOnResolve()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ReviewSettings:MinCommentLength"] = "200",
            ["ReviewSettings:MaxCommentLength"] = "100"
        });
        services.AddReviewSettings(configuration);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ReviewSettings>>();

        var exception = Should.Throw<OptionsValidationException>(() => _ = options.Value);

        exception.Failures.ShouldContain(
            failure => failure.Contains("MinCommentLength", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AddReviewSettings_WhenPurchaseReviewWindowDaysBelowRange_ThrowsOptionsValidationExceptionOnResolve()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ReviewSettings:PurchaseReviewWindowDays"] = "0"
        });
        services.AddReviewSettings(configuration);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ReviewSettings>>();

        Should.Throw<OptionsValidationException>(() => _ = options.Value);
    }

    [Fact]
    public void AddReviewSettings_WhenMinCommentLengthEqualsMaxCommentLength_ResolvesSuccessfully()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ReviewSettings:MinCommentLength"] = "50",
            ["ReviewSettings:MaxCommentLength"] = "50"
        });
        services.AddReviewSettings(configuration);
        var provider = services.BuildServiceProvider();

        var settings = provider.GetRequiredService<IOptions<ReviewSettings>>().Value;

        settings.MinCommentLength.ShouldBe(50);
        settings.MaxCommentLength.ShouldBe(50);
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> data) =>
        new ConfigurationBuilder().AddInMemoryCollection(data).Build();
}
