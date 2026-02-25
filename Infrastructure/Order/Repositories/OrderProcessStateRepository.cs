namespace Infrastructure.Order.Repositories;

public class OrderProcessStateRepository : IOrderProcessStateRepository
{
    private readonly DBContext _context;

    public OrderProcessStateRepository(DBContext context)
    {
        _context = context;
    }

    public async Task<OrderProcessState?> GetByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        return await _context.Set<OrderProcessState>()
            .FirstOrDefaultAsync(s => s.OrderId == orderId, ct);
    }

    public async Task AddAsync(OrderProcessState state, CancellationToken ct = default)
    {
        await _context.Set<OrderProcessState>().AddAsync(state, ct);
    }

    public void Update(OrderProcessState state)
    {
        _context.Set<OrderProcessState>().Update(state);
    }
}