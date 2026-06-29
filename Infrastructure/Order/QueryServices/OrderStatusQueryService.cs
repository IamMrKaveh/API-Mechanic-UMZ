using Application.Order.Contracts;
using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;

namespace Infrastructure.Order.QueryServices;

public sealed class OrderStatusQueryService(DBContext context) : IOrderStatusQueryService
{
    public async Task<IReadOnlyList<OrderStatusDto>> GetAllAsync(
        bool? onlyActive = null,
        CancellationToken ct = default)
    {
        var query = context.OrderStatuses.AsNoTracking();

        if (onlyActive == true)
            query = query.Where(s => s.IsActive);

        return await query
            .OrderBy(s => s.SortOrder)
            .Select(s => new OrderStatusDto
            {
                Id = s.Id.Value,
                Name = s.Name,
                DisplayName = s.DisplayName,
                Icon = s.Icon,
                Color = s.Color,
                SortOrder = s.SortOrder,
                IsActive = s.IsActive,
                IsDefault = s.IsDefault,
                AllowCancel = s.AllowCancel,
                AllowEdit = s.AllowEdit,
                RowVersion = s.RowVersion == null ? null : Convert.ToBase64String(s.RowVersion)
            })
            .ToListAsync(ct);
    }

    public async Task<OrderStatusDto?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var statusId = OrderStatusId.From(id);

        return await context.OrderStatuses
            .AsNoTracking()
            .Where(s => s.Id == statusId)
            .Select(s => new OrderStatusDto
            {
                Id = s.Id.Value,
                Name = s.Name,
                DisplayName = s.DisplayName,
                Icon = s.Icon,
                Color = s.Color,
                SortOrder = s.SortOrder,
                IsActive = s.IsActive,
                IsDefault = s.IsDefault,
                AllowCancel = s.AllowCancel,
                AllowEdit = s.AllowEdit,
                RowVersion = s.RowVersion == null ? null : Convert.ToBase64String(s.RowVersion)
            })
            .FirstOrDefaultAsync(ct);
    }
}