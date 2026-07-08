using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Interfaces;

public interface IWalletTransferRepository
{
    Task AddAsync(WalletTransfer transfer, CancellationToken ct = default);

    void Update(WalletTransfer transfer);

    Task<WalletTransfer?> GetByIdAsync(
        WalletTransferId id,
        CancellationToken ct = default);

    Task<WalletTransfer?> GetByIdForUpdateAsync(
        WalletTransferId id,
        CancellationToken ct = default);

    Task<decimal> SumCompletedAmountForDayAsync(
        UserId fromUserId,
        DateTime dayUtc,
        CancellationToken ct = default);

    Task<int> CountRecentPendingByUserAsync(
        UserId fromUserId,
        TimeSpan window,
        CancellationToken ct = default);
}