using Application.Wallet.Options;
using Microsoft.Extensions.Configuration;

namespace Application.Common.DependencyInjection;

public static class WalletTransferOptionsRegistration
{
    public static IServiceCollection AddWalletTransferOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<WalletTransferOptions>(
            configuration.GetSection(WalletTransferOptions.SectionName));
        return services;
    }
}