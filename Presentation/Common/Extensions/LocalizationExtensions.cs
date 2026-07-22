using System.Globalization;
using Application.Localization.Options;
using Microsoft.AspNetCore.Localization;

namespace Presentation.Common.Extensions;

public static class LocalizationExtensions
{
    public static IServiceCollection AddApplicationLocalization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<LocalizationOptions>()
            .Bind(configuration.GetSection(LocalizationOptions.SectionName))
            .ValidateDataAnnotations();

        var options = configuration.GetSection(LocalizationOptions.SectionName).Get<LocalizationOptions>()
            ?? new LocalizationOptions();

        services.Configure<RequestLocalizationOptions>(o =>
        {
            var cultures = options.SupportedCultures
                .Select(name => new CultureInfo(name))
                .ToArray();

            o.DefaultRequestCulture = new RequestCulture(options.DefaultCulture, options.DefaultCulture);
            o.SupportedCultures = cultures;
            o.SupportedUICultures = cultures;
            o.FallBackToParentCultures = options.FallbackToParentCultures;
            o.FallBackToParentUICultures = options.FallbackToParentCultures;

            o.RequestCultureProviders =
            [
                new AcceptLanguageHeaderRequestCultureProvider(),
                new QueryStringRequestCultureProvider(),
                new CookieRequestCultureProvider()
            ];
        });

        return services;
    }

    public static IApplicationBuilder UseApplicationLocalization(this IApplicationBuilder app)
    {
        var localizationOptions = app.ApplicationServices
            .GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        app.UseRequestLocalization(localizationOptions);
        return app;
    }
}
