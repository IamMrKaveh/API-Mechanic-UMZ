using Application.Order.Features.Shared;

namespace Infrastructure.Order.QueryServices;

public sealed class OrderStatusQueryService(DBContext context) : IOrderStatusQueryService
{
    public async Task<IReadOnlyList<OrderStatusDto>> GetAllAsync(
        CancellationToken ct = default)
    {
        return await context.OrderStatuses
            .AsNoTracking()
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
                AllowCancel = s.AllowCancel,
                AllowEdit = s.AllowEdit
            })
            .ToListAsync(ct);
    }
}