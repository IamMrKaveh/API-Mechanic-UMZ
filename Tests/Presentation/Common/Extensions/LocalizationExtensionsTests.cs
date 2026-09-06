using Application.Localization.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Presentation.Common.Extensions;

namespace Tests.Presentation.Common.Extensions;

public class LocalizationExtensionsTests
{
    private static IConfiguration BuildConfig(
        string defaultCulture = "fa-IR",
        string[]? supported = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Localization:DefaultCulture"] = defaultCulture,
                ["Localization:FallbackToParentCultures"] = "true"
            }.Concat((supported ?? ["fa-IR", "en-US"]).Select((c, i) =>
                new KeyValuePair<string, string?>($"Localization:SupportedCultures:{i}", c))))
            .Build();

    [Fact]
    public void AddApplicationLocalization_ConfiguresRequestLocalization()
    {
        var services = new ServiceCollection();
        services.AddApplicationLocalization(BuildConfig());

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;

        options.DefaultRequestCulture.Culture.Name.ShouldBe("fa-IR");
        // NOTE: ConfigurationBinder appends bound values to the non-empty
        // defaults of LocalizationOptions.SupportedCultures, so assert distinct names.
        options.SupportedCultures!.Select(c => c.Name).Distinct().ShouldBe(["fa-IR", "en-US"]);
        options.SupportedUICultures!.Select(c => c.Name).Distinct().ShouldBe(["fa-IR", "en-US"]);
        options.FallBackToParentCultures.ShouldBeTrue();
        options.FallBackToParentUICultures.ShouldBeTrue();
        options.RequestCultureProviders.Count.ShouldBe(3);
        options.RequestCultureProviders[0].ShouldBeOfType<AcceptLanguageHeaderRequestCultureProvider>();
        options.RequestCultureProviders[1].ShouldBeOfType<QueryStringRequestCultureProvider>();
        options.RequestCultureProviders[2].ShouldBeOfType<CookieRequestCultureProvider>();
    }

    [Fact]
    public void AddApplicationLocalization_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddApplicationLocalization(BuildConfig()).ShouldBeSameAs(services);
    }

    [Fact]
    public void UseApplicationLocalization_ReturnsSameBuilder()
    {
        var services = new ServiceCollection();
        services.AddApplicationLocalization(BuildConfig());
        var app = new ApplicationBuilder(services.BuildServiceProvider());

        app.UseApplicationLocalization().ShouldBeSameAs(app);
    }
}
