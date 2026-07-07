using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Interfaces;

public interface IWalletFraudAlertRepository
{
    Task AddAsync(WalletFraudAlert alert, CancellationToken ct = default);

    Task<WalletFraudAlert?> GetByIdAsync(WalletFraudAlertId id, CancellationToken ct = default);

    void Update(WalletFraudAlert alert);

    Task<bool> HasRecentAlertAsync(
        WalletId walletId,
        string ruleName,
        TimeSpan cooldown,
        CancellationToken ct = default);
}