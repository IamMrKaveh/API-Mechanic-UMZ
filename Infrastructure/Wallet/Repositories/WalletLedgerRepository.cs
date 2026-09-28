using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Interfaces;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletLedgerRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<WalletLedgerEntry, Domain.Wallet.ValueObjects.WalletLedgerEntryId>(context), IWalletLedgerRepository
{
    public override async Task AddAsync(WalletLedgerEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await Context.WalletLedgerEntries.AddAsync(entry, ct);
    }

    public async Task<bool> HasIdempotencyKeyAsync(
        string? idempotencyKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return false;

        return await Context.WalletLedgerEntries
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

        return await Context.WalletLedgerEntries
            .IgnoreQueryFilters()
            .AnyAsync(e => e.OwnerId == ownerId && e.IdempotencyKey == idempotencyKey, ct);
    }
}
