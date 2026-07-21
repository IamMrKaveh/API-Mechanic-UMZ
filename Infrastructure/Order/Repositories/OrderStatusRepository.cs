using Domain.Order.Entities;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Infrastructure.Order.Repositories;

public sealed class OrderStatusRepository(DBContext context) : IOrderStatusRepository
{
    public async Task<OrderStatus?> GetByIdAsync(
        OrderStatusId id,
        CancellationToken ct = default)
        => await context.OrderStatuses.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<OrderStatus?> GetDefaultAsync(
        CancellationToken ct = default)
        => await context.OrderStatuses.FirstOrDefaultAsync(s => s.IsDefault, ct);

    public async Task<bool> IsInUseAsync(
        OrderStatusId id,
        CancellationToken ct = default)
    {
        var status = await context.OrderStatuses
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(status))
            return false;

        return await context.Orders
            .AsNoTracking()
            .AnyAsync(o => o.Status.Value == status, ct);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        OrderStatusId? excludeId = null,
        CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        var query = context.OrderStatuses
            .AsNoTracking()
            .Where(s => s.Name == trimmed);

        if (excludeId is not null)
            query = query.Where(s => s.Id != excludeId);

        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(
        OrderStatus orderStatus,
        CancellationToken ct = default)
        => await context.OrderStatuses.AddAsync(orderStatus, ct);

    public void Update(OrderStatus orderStatus, byte[]? rowVersion = null)
    {
        context.OrderStatuses.Update(orderStatus);

        if (rowVersion is not null && rowVersion.Length > 0)
            SetOriginalRowVersion(orderStatus, rowVersion);
    }

    public void Remove(OrderStatus orderStatus)
        => context.OrderStatuses.Remove(orderStatus);

    public void SetOriginalRowVersion(OrderStatus entity, byte[] rowVersion)
    {
        if (rowVersion is null || rowVersion.Length == 0)
            return;
        context.Entry(entity).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }
}
