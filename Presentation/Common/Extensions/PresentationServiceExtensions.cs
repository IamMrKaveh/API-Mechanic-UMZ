using Application.Common.Interfaces;
using Presentation.Common.Services;

namespace Presentation.Common.Extensions;

public static class PresentationServiceExtensions
{
    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPresentationControllers();
        services.AddPresentationOptions(configuration);
        services.AddPresentationInternalServices();
        services.AddCustomCors(configuration);
        services.AddTrustedForwardedHeaders(configuration);
        services.RegisterValidation();

        return services;
    }

    private static IServiceCollection AddPresentationInternalServices(
        this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<OtpRateLimitFilter>();
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }

    private static IServiceCollection RegisterValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollection).Assembly);

        return services;
    }
}