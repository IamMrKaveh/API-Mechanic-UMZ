using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Interfaces;

public interface IWalletDebitRequestRepository
{
    Task<WalletDebitRequest?> GetByIdAsync(WalletDebitRequestId id, CancellationToken ct = default);

    Task<IReadOnlyList<WalletDebitRequest>> GetByOwnerAsync(
        UserId ownerId,
        WalletDebitRequestStatus? status = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<WalletDebitRequest>> GetPendingByOwnerAsync(
        UserId ownerId,
        CancellationToken ct = default);
}
