using Domain.Wallet.Interfaces;

namespace Infrastructure.Wallet.Repositories;

public class WalletRepository(DBContext context) : IWalletRepository
{
    private readonly DBContext _context = context;

    public async Task<Domain.Wallet.Aggregates.Wallet?> GetByUserIdAsync(
        int userId,
        CancellationToken ct = default)
    {
        return await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);
    }

    public async Task<Domain.Wallet.Aggregates.Wallet?> GetByUserIdForUpdateAsync(
        int userId,
        CancellationToken ct = default)
    {
        var wallet = await _context.Wallets
            .FromSqlRaw(
                "SELECT * FROM \"Wallets\" WHERE \"UserId\" = {0} FOR UPDATE",
                userId)
            .FirstOrDefaultAsync(ct);

        if (wallet == null) return null;

        await _context.Entry(wallet)
            .Collection(w => w.Reservations)
            .Query()
            .Where(r => r.Status == WalletReservationStatus.Pending)
            .LoadAsync(ct);

        return wallet;
    }

    public async Task AddAsync(
        Domain.Wallet.Aggregates.Wallet wallet,
        CancellationToken ct = default)
    {
        await _context.Wallets.AddAsync(wallet, ct);
    }

    public void Update(Domain.Wallet.Aggregates.Wallet wallet)
    {
        _context.Wallets.Update(wallet);
    }

    public async Task<bool> ExistsForUserAsync(
        int userId,
        CancellationToken ct = default)
    {
        return await _context.Wallets.AnyAsync(w => w.UserId == userId, ct);
    }

    public async Task<bool> HasIdempotencyKeyAsync(
        int userId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        return await _context.WalletLedgerEntries
            .AnyAsync(e => e.UserId == userId && e.IdempotencyKey == idempotencyKey, ct);
    }

    public async Task<List<ExpiredReservationProjection>> GetExpiredReservationBatchAsync(
        int batchSize,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        return await _context.WalletReservations
            .Where(r =>
                r.Status == WalletReservationStatus.Pending &&
                r.ExpiresAt.HasValue &&
                r.ExpiresAt.Value < now)
            .OrderBy(r => r.ExpiresAt)
            .Take(batchSize)
            .Select(r => new ExpiredReservationProjection(
                r.Id,
                r.WalletId,
                r.Wallet.UserId,
                r.Amount,
                r.OrderId))
            .ToListAsync(ct);
    }

    public async Task<bool> ExpireReservationAsync(
        int reservationId,
        int walletId,
        decimal amount,
        CancellationToken ct = default)
    {
        var affectedRows = await _context.Database.ExecuteSqlRawAsync(
            @"UPDATE ""WalletReservations""
              SET ""Status"" = 'Expired', ""UpdatedAt"" = {0}
              WHERE ""Id"" = {1} AND ""Status"" = 'Pending';

              UPDATE ""Wallets""
              SET ""ReservedBalance"" = GREATEST(""ReservedBalance"" - {2}, 0),
                  ""UpdatedAt"" = {0}
              WHERE ""Id"" = {3} AND ""ReservedBalance"" >= {2};",
            DateTime.UtcNow,
            reservationId,
            amount,
            walletId,
            ct);

        return affectedRows > 0;
    }
}