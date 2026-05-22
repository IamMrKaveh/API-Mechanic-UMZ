using Domain.User.ValueObjects;

namespace Domain.Wallet.Interfaces;

public interface IWalletRepository
{
    Task<Aggregates.Wallet?> GetByUserIdAsync(
        UserId ownerId,
        CancellationToken ct = default);

    Task<Aggregates.Wallet?> GetByUserIdForUpdateAsync(
        UserId ownerId,
        CancellationToken ct = default);

    Task AddAsync(
        Aggregates.Wallet wallet,
        CancellationToken ct = default);

    void Update(
        Aggregates.Wallet wallet);

    Task<bool> HasIdempotencyKeyAsync(
        UserId ownerId,
        string idempotencyKey,
        CancellationToken ct = default);
}