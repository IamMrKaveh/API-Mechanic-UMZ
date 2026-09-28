using Domain.Common.Interfaces;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Interfaces;

public interface IWalletFraudAlertRepository : IRepository<WalletFraudAlert, WalletFraudAlertId>
{
    Task<bool> HasRecentAlertAsync(
        WalletId walletId,
        string ruleName,
        TimeSpan cooldown,
        CancellationToken ct = default);
}