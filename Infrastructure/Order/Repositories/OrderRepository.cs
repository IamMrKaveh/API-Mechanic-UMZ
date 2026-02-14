namespace Infrastructure.Order.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly LedkaContext _context;

    public OrderRepository(LedkaContext context)
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
            .Include(o => o.ShippingMethod)
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

    public void Update(Domain.Order.Order order)
    {
        _context.Orders.Update(order);
    }

    public void SetOriginalRowVersion(Domain.Order.Order entity, byte[] rowVersion)
    {
        _context.Entry(entity).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }

    public Task<Domain.Order.Order?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Domain.Order.Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HasActiveOrdersAsync(int userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}