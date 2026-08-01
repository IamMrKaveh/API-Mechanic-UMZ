using Domain.User.ValueObjects;
using Domain.Wallet.Entities;

namespace Domain.Wallet.Interfaces;

public interface IWalletLedgerRepository
{
    Task AddAsync(WalletLedgerEntry entry, CancellationToken ct = default);

    Task<bool> HasIdempotencyKeyAsync(
        string? idempotencyKey,
        CancellationToken ct = default);

    Task<bool> HasIdempotencyKeyAsync(
        UserId ownerId,
        string idempotencyKey,
        CancellationToken ct = default);
}
