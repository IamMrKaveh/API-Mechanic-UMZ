using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Interfaces;

public interface IWalletTopUpRepository
{
    Task AddAsync(WalletTopUp topUp, CancellationToken ct = default);

    void Update(WalletTopUp topUp);

    Task<WalletTopUp?> GetByIdAsync(WalletTopUpId id, CancellationToken ct = default);

    Task<WalletTopUp?> GetByAuthorityAsync(string authority, CancellationToken ct = default);

    Task<IReadOnlyList<WalletTopUp>> GetPendingOlderThanAsync(DateTime cutoffUtc, int batchSize, CancellationToken ct = default);

    Task<IReadOnlyList<WalletTopUp>> GetByUserIdAsync(UserId userId, int page, int pageSize, CancellationToken ct = default);
}