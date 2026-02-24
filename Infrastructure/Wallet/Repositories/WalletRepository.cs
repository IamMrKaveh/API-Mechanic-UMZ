namespace Infrastructure.Wallet.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly DBContext _context;

    public WalletRepository(DBContext context)
    {
        _context = context;
    }

    /// <summary>Loads snapshot only – no ledger, no reservations.</summary>
    public async Task<Domain.Wallet.Wallet?> GetByUserIdAsync(
        int userId,
        CancellationToken ct = default)
    {
        return await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);
    }

    /// <summary>
    /// Loads wallet snapshot with active reservations and acquires a database-level
    /// pessimistic write lock (SELECT … FOR UPDATE) so concurrent debits/credits
    /// are serialized for the same wallet row.
    /// </summary>
    public async Task<Domain.Wallet.Wallet?> GetByUserIdForUpdateAsync(
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

    /// <summary>Paginated ledger query – never loads via the Wallet aggregate.</summary>
    public async Task<(List<WalletLedgerEntry> Items, int TotalCount)> GetLedgerPageAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.WalletLedgerEntries
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(
        Domain.Wallet.Wallet wallet,
        CancellationToken ct = default)
    {
        await _context.Wallets.AddAsync(wallet, ct);
    }

    public void Update(Domain.Wallet.Wallet wallet)
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

    /// <summary>
    /// Returns a batch of expired pending reservations as lightweight projections.
    /// No Wallet aggregate is loaded; only WalletReservation rows are queried.
    /// </summary>
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

    /// <summary>
    /// Atomically marks the reservation as Expired and decrements the wallet's
    /// ReservedBalance using a single UPDATE statement with optimistic concurrency
    /// on the wallet RowVersion. Returns false if another process already expired it.
    /// </summary>
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