namespace Infrastructure.Order.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly Persistence.Context.DBContext _context;

    public OrderRepository(Persistence.Context.DBContext context)
    {
        _context = context;
    }

    public async Task<Domain.Order.Order?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<Domain.Order.Order?> GetByIdWithItemsAsync(int id, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Shipping)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<Domain.Order.Order?> GetByIdempotencyKeyAsync(string key, int userId, CancellationToken ct = default)
    {
        return await _context.Orders
            .FirstOrDefaultAsync(o => o.IdempotencyKey == key && o.UserId == userId, ct);
    }

    public async Task<bool> ExistsByIdempotencyKeyAsync(string key, CancellationToken ct = default)
    {
        return await _context.Orders
            .AnyAsync(o => o.IdempotencyKey == key, ct);
    }

    public async Task<IEnumerable<Domain.Order.Order>> GetExpiredUnpaidOrdersAsync(
        DateTime cutoffTime, int maxCount, CancellationToken ct = default)
    {
        return await _context.Orders
            .Where(o => o.Status == OrderStatusValue.Pending
                        && o.CreatedAt < cutoffTime
                        && !o.IsDeleted)
            .OrderBy(o => o.CreatedAt)
            .Take(maxCount)
            .Include(o => o.OrderItems)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Domain.Order.Order order, CancellationToken ct = default)
    {
        await _context.Orders.AddAsync(order, ct);
    }

    public async Task UpdateAsync(
        Domain.Order.Order order,
        CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            _context.Orders.Update(order);

            await _context.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public void SetOriginalRowVersion(Domain.Order.Order entity, byte[] rowVersion)
    {
        _context.Entry(entity).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }

    public async Task<Domain.Order.Order?> GetByIdempotencyKeyAsync(
        string key,
        CancellationToken ct)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(
                o => o.IdempotencyKey == key,
                ct);
    }

    public async Task<Domain.Order.Order?> GetByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(
                o => o.OrderNumber == orderNumber,
                cancellationToken);
    }

    public async Task<bool> HasActiveOrdersAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        return await _context.Orders
            .AnyAsync(
                o => o.UserId == userId &&
                     o.Status != OrderStatus.Statuses.Delivered &&
                     o.Status != OrderStatus.Statuses.Cancelled,
                cancellationToken);
    }

    public async Task<Domain.Order.Order?> GetByOrderItemIdAsync(
    int orderItemId,
    CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(
                o => o.OrderItems.Any(oi => oi.Id == orderItemId),
                ct);
    }

    public Task<IEnumerable<Domain.Order.Order>> GetExpirableOrdersAsync(DateTime expiryThreshold, IEnumerable<string> statuses, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}