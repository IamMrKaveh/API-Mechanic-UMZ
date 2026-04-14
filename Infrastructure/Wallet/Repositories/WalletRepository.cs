using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Interfaces;
using Domain.Wallet.Projections;
using Domain.Wallet.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletRepository(DBContext context) : IWalletRepository
{
    public async Task<Domain.Wallet.Aggregates.Wallet?> GetByIdAsync(
        WalletId walletId, CancellationToken ct = default)
        => await context.Wallets
            .Include(w => w.ActiveReservations)
            .FirstOrDefaultAsync(w => w.Id == walletId, ct);

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

    public async Task<IReadOnlyList<Domain.Wallet.Aggregates.Wallet>> GetAllActiveAsync(
        CancellationToken ct = default)
    {
        var result = await context.Wallets
            .Where(w => w.IsActive)
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task<bool> ExistsForUserAsync(UserId userId, CancellationToken ct = default)
        => await context.Wallets.AnyAsync(w => w.OwnerId == userId, ct);

    public async Task<bool> HasIdempotencyKeyAsync(
        UserId userId, string idempotencyKey, CancellationToken ct = default)
        => await context.WalletLedgerEntries.AnyAsync(
            e => e.OwnerId == userId && e.IdempotencyKey == idempotencyKey, ct);

    public async Task<IReadOnlyList<WalletLedgerEntry>> GetLedgerEntriesAsync(
        WalletId walletId, CancellationToken ct = default)
    {
        var result = await context.WalletLedgerEntries
            .Where(e => e.WalletId == walletId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task<WalletLedgerEntry?> GetLedgerEntryByIdAsync(
        WalletLedgerEntryId id, CancellationToken ct = default)
        => await context.WalletLedgerEntries.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<WalletLedgerEntry?> GetLedgerEntryByReferenceAsync(
        WalletId walletId, string referenceId, CancellationToken ct = default)
        => await context.WalletLedgerEntries.FirstOrDefaultAsync(
            e => e.WalletId == walletId && e.ReferenceId == referenceId, ct);

    public async Task AddLedgerEntryAsync(
        WalletLedgerEntry entry, CancellationToken ct = default)
        => await context.WalletLedgerEntries.AddAsync(entry, ct);

    public async Task<IReadOnlyList<ExpiredReservationProjection>> GetExpiredReservationBatchAsync(
        int batchSize, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var result = await context.WalletReservations
            .AsNoTracking()
            .Where(r => r.Status == Domain.Wallet.Enums.WalletReservationStatus.Pending
                        && r.ExpiresAt.HasValue && r.ExpiresAt.Value <= now)
            .Take(batchSize)
            .Select(r => new ExpiredReservationProjection
            {
                ReservationId = r.Id.Value,
                WalletId = r.Wallet.Id.Value,
                Amount = r.Amount.Amount,
                OrderId = r.OrderId.Value,
                UserId = r.Wallet.OwnerId.Value
            })
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task ExpireReservationAsync(
        WalletReservationId reservationId,
        WalletId walletId,
        Money amount,
        CancellationToken ct = default)
    {
        var reservation = await context.WalletReservations
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct);

        if (reservation is null) return;

        reservation.Expire();
        context.WalletReservations.Update(reservation);
    }

    public async Task<WalletReservation?> GetReservationByIdAsync(
        WalletReservationId id, CancellationToken ct = default)
        => await context.WalletReservations.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(
        Domain.Wallet.Aggregates.Wallet wallet, CancellationToken ct = default)
        => await context.Wallets.AddAsync(wallet, ct);

    public void Update(Domain.Wallet.Aggregates.Wallet wallet)
        => context.Wallets.Update(wallet);

    public void SetOriginalRowVersion(Domain.Wallet.Aggregates.Wallet wallet, byte[] rowVersion)
        => context.Entry(wallet).Property(e => e.RowVersion).OriginalValue = rowVersion;
}