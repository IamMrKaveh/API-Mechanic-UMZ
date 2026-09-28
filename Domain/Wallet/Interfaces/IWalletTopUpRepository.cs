using Domain.Common.Interfaces;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Interfaces;

public interface IWalletTopUpRepository : IRepository<WalletTopUp, WalletTopUpId>
{
    Task<WalletTopUp?> GetByAuthorityAsync(string authority, CancellationToken ct = default);

    Task<IReadOnlyList<WalletTopUp>> GetPendingOlderThanAsync(DateTime cutoffUtc, int batchSize, CancellationToken ct = default);

    Task<IReadOnlyList<WalletTopUp>> GetByUserIdAsync(UserId userId, int page, int pageSize, CancellationToken ct = default);
}