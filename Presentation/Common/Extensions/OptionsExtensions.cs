using Infrastructure.Security.Settings;
using Presentation.Common.Options;

namespace Presentation.Common.Extensions;

public static class OptionsExtensions
{
    public static IServiceCollection AddPresentationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
        });

        services.Configure<SecurityHeadersOptions>(
            configuration.GetSection("SecurityHeaders"));

        services.Configure<SecuritySettings>(
            configuration.GetSection(SecuritySettings.SectionName));

        return services;
    }
}