using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletRepository(DBContext context) : IWalletRepository
{
    public async Task<Domain.Wallet.Aggregates.Wallet?> GetByIdAsync(
        WalletId walletId,
        CancellationToken ct = default)
    {
        return await context.Wallets
            .Include(w => w.LedgerEntries)
            .Include(w => w.Reservations)
            .FirstOrDefaultAsync(w => w.Id == walletId, ct);
    }

    public async Task<Domain.Wallet.Aggregates.Wallet?> GetByUserIdAsync(
        UserId userId,
        CancellationToken ct = default)
    {
        return await context.Wallets
            .Include(w => w.LedgerEntries)
            .Include(w => w.Reservations)
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);
    }

    public async Task<Domain.Wallet.Aggregates.Wallet?> GetByUserIdForUpdateAsync(
        UserId userId,
        CancellationToken ct = default)
    {
        return await context.Wallets
            .Include(w => w.Reservations)
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);
    }

    public async Task AddAsync(Domain.Wallet.Aggregates.Wallet wallet, CancellationToken ct = default)
    {
        await context.Wallets.AddAsync(wallet, ct);
    }

    public void Update(Domain.Wallet.Aggregates.Wallet wallet)
    {
        context.Wallets.Update(wallet);
    }

    public void SetOriginalRowVersion(Domain.Wallet.Aggregates.Wallet wallet, byte[] rowVersion)
    {
        context.Entry(wallet).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }
}