using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Interfaces;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletLedgerRepository(DBContext context) : IWalletLedgerRepository
{
    public async Task AddAsync(WalletLedgerEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await context.WalletLedgerEntries.AddAsync(entry, ct);
    }

    public async Task<bool> HasIdempotencyKeyAsync(
        string? idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return false;

        return await context.WalletLedgerEntries
            .IgnoreQueryFilters()
            .AnyAsync(e => e.IdempotencyKey == idempotencyKey, ct);
    }

    public async Task<bool> HasIdempotencyKeyAsync(
        UserId ownerId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return false;

        return await context.WalletLedgerEntries
            .IgnoreQueryFilters()
            .AnyAsync(e => e.OwnerId == ownerId && e.IdempotencyKey == idempotencyKey, ct);
    }
}
