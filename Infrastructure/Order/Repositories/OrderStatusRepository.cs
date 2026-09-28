using Domain.Order.Entities;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using SharedKernel.Exceptions;

namespace Infrastructure.Order.Repositories;

public sealed class OrderStatusRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<OrderStatus, OrderStatusId>(context), IOrderStatusRepository
{
    public async Task<OrderStatus?> GetDefaultAsync(
        CancellationToken ct = default)
        => await Context.OrderStatuses.FirstOrDefaultAsync(s => s.IsDefault, ct);

    public async Task<bool> IsInUseAsync(
        OrderStatusId id,
        CancellationToken ct = default)
    {
        var name = await Context.OrderStatuses
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(name))
            return false;

        OrderStatusValue statusValue;
        try
        {
            statusValue = OrderStatusValue.From(name);
        }
        catch (DomainException)
        {
            return false;
        }

        return await Context.Orders
            .AsNoTracking()
            .AnyAsync(o => o.Status == statusValue, ct);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        OrderStatusId? excludeId = null,
        CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        var query = Context.OrderStatuses
            .AsNoTracking()
            .Where(s => s.Name == trimmed);

        if (excludeId is not null)
            query = query.Where(s => s.Id != excludeId);

        return await query.AnyAsync(ct);
    }

    public void Update(OrderStatus orderStatus, byte[]? rowVersion = null)
    {
        base.Update(orderStatus);

        if (rowVersion is not null && rowVersion.Length > 0)
            SetOriginalRowVersion(orderStatus, rowVersion);
    }

    public void Remove(OrderStatus orderStatus)
        => Context.OrderStatuses.Remove(orderStatus);

    public void SetOriginalRowVersion(OrderStatus entity, byte[] rowVersion)
    {
        if (rowVersion is null || rowVersion.Length == 0)
            return;
        Context.Entry(entity).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }
}
