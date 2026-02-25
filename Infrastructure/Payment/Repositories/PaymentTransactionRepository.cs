namespace Infrastructure.Payment.Repositories;

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly Persistence.Context.DBContext _context;

    public PaymentTransactionRepository(Persistence.Context.DBContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken ct = default)
    {
        await _context.PaymentTransactions.AddAsync(transaction, ct);
    }

    public async Task<PaymentTransaction?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<PaymentTransaction?> GetByAuthorityAsync(string authority, CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.Authority.Value == authority, ct);
    }

    public async Task<PaymentTransaction?> GetByAuthorityWithOrderAsync(string authority, CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.Authority.Value == authority, ct);
    }

    public async Task<IEnumerable<PaymentTransaction>> GetPendingExpiredTransactionsAsync(
        DateTime cutoffTime,
        CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .Where(t => t.Status.Value == "Pending" && t.ExpiresAt < cutoffTime && !t.IsDeleted)
            .ToListAsync(ct);
    }

    public async Task<(IEnumerable<PaymentTransaction> Transactions, int TotalCount)> GetPagedAsync(
        int? orderId = null,
        int? userId = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = _context.PaymentTransactions
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        if (orderId.HasValue)
            query = query.Where(t => t.OrderId == orderId.Value);

        if (userId.HasValue)
            query = query.Where(t => t.UserId == userId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status.Value == status);

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IEnumerable<PaymentTransaction>> GetByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .Where(t => t.OrderId == orderId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<PaymentTransaction>> GetSuccessfulByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .Where(t => t.OrderId == orderId && t.Status.Value == "Success" && !t.IsDeleted)
            .ToListAsync(ct);
    }

    public async Task<bool> HasSuccessfulPaymentAsync(int orderId, CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .AnyAsync(t => t.OrderId == orderId && t.Status.Value == "Success" && !t.IsDeleted, ct);
    }

    public async Task<PaymentTransaction?> GetLatestByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .Where(t => t.OrderId == orderId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> HasPendingPaymentAsync(int orderId, CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .AnyAsync(t => t.OrderId == orderId && t.Status.Value == "Pending" && !t.IsDeleted, ct);
    }

    public async Task<PaymentStatistics> GetStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var query = _context.PaymentTransactions
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        var all = await query.ToListAsync(ct);

        return PaymentStatistics.Create(
            totalTransactions: all.Count,
            successfulTransactions: all.Count(t => t.Status.Value == "Success"),
            failedTransactions: all.Count(t => t.Status.Value == "Failed"),
            pendingTransactions: all.Count(t => t.Status.Value == "Pending"),
            expiredTransactions: all.Count(t => t.Status.Value == "Expired"),
            refundedTransactions: all.Count(t => t.Status.Value == "Refunded"),
            totalSuccessfulAmount: all.Where(t => t.Status.Value == "Success").Sum(t => t.Amount.Amount),
            totalRefundedAmount: all.Where(t => t.Status.Value == "Refunded").Sum(t => t.Amount.Amount),
            totalFees: all.Sum(t => t.Fee)
        );
    }

    public void Update(PaymentTransaction transaction)
    {
        _context.PaymentTransactions.Update(transaction);
    }

    public void SetOriginalRowVersion(PaymentTransaction entity, byte[] rowVersion)
    {
        _context.Entry(entity).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }

    public async Task<PaymentTransaction?> GetVerifiedByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .Where(t => t.OrderId == orderId && t.Status.Value == "Success" && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PaymentTransaction?> GetActiveByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .Where(t => t.OrderId == orderId
                        && (t.Status.Value == "Pending" || t.Status.Value == "Processing")
                        && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }
}