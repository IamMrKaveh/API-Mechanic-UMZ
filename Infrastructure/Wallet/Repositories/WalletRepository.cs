using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletRepository(DBContext context) : IWalletRepository
{
    public async Task<Domain.Wallet.Aggregates.Wallet?> GetByUserIdAsync(
        UserId userId, CancellationToken ct = default)
        => await context.Wallets
            .Include(w => w.ActiveReservations)
            .FirstOrDefaultAsync(w => w.OwnerId == userId, ct);

    public async Task<Domain.Wallet.Aggregates.Wallet?> GetByUserIdForUpdateAsync(
        UserId userId, CancellationToken ct = default)
        => await context.Wallets
            .Include(w => w.ActiveReservations)
            .FirstOrDefaultAsync(w => w.OwnerId == userId, ct);

    public async Task<bool> HasIdempotencyKeyAsync(
        UserId userId, string idempotencyKey, CancellationToken ct = default)
        => await context.WalletLedgerEntries.AnyAsync(
            e => e.OwnerId == userId && e.IdempotencyKey == idempotencyKey, ct);

    public async Task AddAsync(
        Domain.Wallet.Aggregates.Wallet wallet, CancellationToken ct = default)
        => await context.Wallets.AddAsync(wallet, ct);

    public void Update(Domain.Wallet.Aggregates.Wallet wallet)
        => context.Wallets.Update(wallet);
}