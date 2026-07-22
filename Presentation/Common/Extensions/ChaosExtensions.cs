using Infrastructure.Chaos.Options;

namespace Presentation.Common.Extensions;

public static class ChaosExtensions
{
    public static IServiceCollection AddChaosEngineering(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<ChaosOptions>()
            .Bind(configuration.GetSection(ChaosOptions.SectionName))
            .Configure(options =>
            {
                if (environment.IsProduction())
                {
                    typeof(ChaosOptions)
                        .GetProperty(nameof(ChaosOptions.IsEnabled))!
                        .SetValue(options, false);
                }
            })
            .ValidateDataAnnotations();

        return services;
    }
}
