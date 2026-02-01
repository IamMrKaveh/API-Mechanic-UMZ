using Infrastructure.Persistence.Interface.Payment;

namespace Infrastructure.Persistence.Repositories.Payment;

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly LedkaContext _context;

    public PaymentTransactionRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<PaymentTransaction?> GetByIdAsync(int id)
    {
        return await _context.PaymentTransactions
            .Include(pt => pt.Order)
            .FirstOrDefaultAsync(pt => pt.Id == id);
    }

    public async Task<PaymentTransaction?> GetByAuthorityAsync(string authority)
    {
        return await _context.PaymentTransactions
            .Include(pt => pt.Order)
            .FirstOrDefaultAsync(pt => pt.Authority == authority);
    }

    public async Task<PaymentTransaction?> GetByAuthorityForUpdateAsync(string authority)
    {
        return await _context.PaymentTransactions
            .FromSqlInterpolated($"SELECT * FROM \"PaymentTransactions\" WHERE \"Authority\" = {authority} FOR UPDATE")
            .Include(pt => pt.Order)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<PaymentTransaction>> GetByOrderIdAsync(int orderId)
    {
        return await _context.PaymentTransactions
            .Where(pt => pt.OrderId == orderId)
            .OrderByDescending(pt => pt.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PaymentTransaction>> GetPendingTransactionsAsync(DateTime cutoffTime)
    {
        return await _context.PaymentTransactions
            .Where(pt => pt.Status == PaymentTransaction.PaymentStatuses.Pending && pt.CreatedAt < cutoffTime)
            .Include(pt => pt.Order)
                .ThenInclude(o => o.OrderItems)
            .ToListAsync();
    }

    public async Task<(IEnumerable<PaymentTransaction> Transactions, int TotalCount)> GetPagedAsync(
        int? orderId,
        int? userId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize)
    {
        var query = _context.PaymentTransactions
            .Include(pt => pt.Order)
            .AsQueryable();

        if (orderId.HasValue)
            query = query.Where(pt => pt.OrderId == orderId.Value);

        if (userId.HasValue)
            query = query.Where(pt => pt.UserId == userId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(pt => pt.Status == status);

        if (fromDate.HasValue)
            query = query.Where(pt => pt.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(pt => pt.CreatedAt <= toDate.Value);

        var totalCount = await query.CountAsync();

        var transactions = await query
            .OrderByDescending(pt => pt.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (transactions, totalCount);
    }

    public async Task AddAsync(PaymentTransaction transaction)
    {
        await _context.PaymentTransactions.AddAsync(transaction);
    }

    public void Update(PaymentTransaction transaction)
    {
        _context.PaymentTransactions.Update(transaction);
    }
}