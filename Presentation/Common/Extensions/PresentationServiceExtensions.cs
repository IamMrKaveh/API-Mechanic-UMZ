using Application.Auth.Contracts;
using Application.Common.Interfaces;
using Presentation.Auth.Services;
using Presentation.Common.Services;
using SharedKernel.Extensions;

namespace Presentation.Common.Extensions;

public static class PresentationServiceExtensions
{
    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddPresentationControllers();
        services.AddPresentationOptions(configuration);
        services.AddPresentationInternalServices();
        services.AddCustomCors(configuration);
        services.AddTrustedForwardedHeaders(configuration);
        services.AddFeatureFlags(configuration);
        services.AddApplicationObservability(configuration, environment);
        services.AddApplicationLocalization(configuration);
        services.AddApplicationAntiforgery();
        services.AddChaosEngineering(configuration, environment);

        return services;
    }

    private static IServiceCollection AddPresentationInternalServices(
        this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IGoogleAuthenticationService, HttpGoogleAuthenticationService>();
        services.AddScoped<OtpRateLimitFilter>();
        services.AddScoped<IMapper, ServiceMapper>();
        services.AddSingleton<IPersianTextNormalizer, PersianTextNormalizerService>();

        return services;
    }
}
