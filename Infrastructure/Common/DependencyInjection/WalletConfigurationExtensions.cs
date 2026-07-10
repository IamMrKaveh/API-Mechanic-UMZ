using Application.Wallet.Options;

namespace Infrastructure.Common.DependencyInjection;

public static class WalletConfigurationExtensions
{
    public static IServiceCollection AddWalletTransferOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<WalletTransferOptions>()
            .Bind(configuration.GetSection(WalletTransferOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}