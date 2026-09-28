using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<Domain.Wallet.Aggregates.Wallet, Domain.Wallet.ValueObjects.WalletId>(context), IWalletRepository
{
    public async Task<Domain.Wallet.Aggregates.Wallet?> GetByUserIdAsync(
        UserId userId, CancellationToken ct = default)
        => await Context.Wallets
            .Include(w => w.Reservations)
            .FirstOrDefaultAsync(w => w.OwnerId == userId, ct);

    public async Task<Domain.Wallet.Aggregates.Wallet?> GetByUserIdForUpdateAsync(
        UserId userId, CancellationToken ct = default)
    {
        var wallet = await Context.Wallets
            .Include(w => w.Reservations)
            .FirstOrDefaultAsync(w => w.OwnerId == userId, ct);

        if (wallet is not null)
        {
            var entry = Context.Entry(wallet);
            entry.Property("xmin").IsModified = false;
            entry.OriginalValues["xmin"] = entry.CurrentValues["xmin"];
        }

        return wallet;
    }

    public async Task<bool> HasIdempotencyKeyAsync(
        UserId userId, string idempotencyKey, CancellationToken ct = default)
        => await Context.WalletLedgerEntries.AnyAsync(
            e => e.OwnerId == userId && e.IdempotencyKey == idempotencyKey, ct);

    public override void Update(Domain.Wallet.Aggregates.Wallet wallet)
    {
        var entry = Context.Entry(wallet);
        switch (entry.State)
        {
            case EntityState.Detached:
                Context.Wallets.Attach(wallet);
                Context.Entry(wallet).State = EntityState.Modified;
                break;

            case EntityState.Unchanged:
                entry.State = EntityState.Modified;
                break;
        }
    }
}
