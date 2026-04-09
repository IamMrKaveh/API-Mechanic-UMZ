using Domain.Order.Entities;
using Domain.Order.Interfaces;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Order.Repositories;

public class OrderStatusRepository(DBContext context) : IOrderStatusRepository
{
    private readonly DBContext _context = context;

    public async Task AddAsync(
        OrderStatus status,
        CancellationToken ct = default)
    {
        await _context.Set<OrderStatus>().AddAsync(status, ct);
    }

    public void Update(OrderStatus status)
    {
        _context.Set<OrderStatus>().Update(status);
    }
}