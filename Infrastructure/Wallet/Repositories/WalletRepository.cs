namespace Infrastructure.Wallet.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly DBContext _context;

    public WalletRepository(
        DBContext context
        )
    {
        _context = context;
    }

    public async Task<Domain.Wallet.Wallet?> GetByUserIdAsync(
        int userId,
        CancellationToken ct = default
        )
    {
        return await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);
    }

    public async Task<Domain.Wallet.Wallet?> GetByUserIdWithEntriesAsync(
        int userId,
        CancellationToken ct = default
        )
    {
        return await _context.Wallets
            .Include(w => w.LedgerEntries)
            .Include(w => w.Reservations)
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);
    }

    public async Task<Domain.Wallet.Wallet?> GetByUserIdForUpdateAsync(
        int userId,
        CancellationToken ct = default
        )
    {
        return await _context.Wallets
            .Include(w => w.Reservations)
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);
    }

    public async Task<(List<WalletLedgerEntry> Items, int TotalCount)> GetLedgerPageAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default
        )
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
        CancellationToken ct = default
        )
    {
        await _context.Wallets.AddAsync(wallet, ct);
    }

    public void Update(
        Domain.Wallet.Wallet wallet
        )
    {
        _context.Wallets.Update(wallet);
    }

    public async Task<bool> ExistsForUserAsync(
        int userId,
        CancellationToken ct = default
        )
    {
        return await _context.Wallets.AnyAsync(w => w.UserId == userId, ct);
    }

    public async Task<bool> HasIdempotencyKeyAsync(
        int userId,
        string idempotencyKey,
        CancellationToken ct = default
        )
    {
        return await _context.WalletLedgerEntries
            .AnyAsync(e => e.UserId == userId && e.IdempotencyKey == idempotencyKey, ct);
    }
}