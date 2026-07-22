using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;
using Microsoft.Net.Http.Headers;

namespace Infrastructure.Order.QueryServices;

public sealed class OrderStatusQueryService(
    DBContext context,
    IHttpContextAccessor httpContextAccessor) : IOrderStatusQueryService
{
    public async Task<IReadOnlyList<OrderStatusDto>> GetAllAsync(bool? onlyActive, CancellationToken ct)
    {
        var query = context.OrderStatuses.AsNoTracking();
        if (onlyActive.HasValue && onlyActive.Value)
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
                AllowEdit = s.AllowEdit
            })
            .ToListAsync(ct);
    }

    public async Task<OrderStatusDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var statusId = OrderStatusId.From(id);
        var dto = await context.OrderStatuses
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
                AllowEdit = s.AllowEdit
            })
            .FirstOrDefaultAsync(ct);

        if (dto is not null)
        {
            var entity = await context.OrderStatuses
                .AsNoTracking()
                .Where(s => s.Id == statusId)
                .Select(s => new { s.RowVersion })
                .FirstOrDefaultAsync(ct);

            if (entity?.RowVersion is not null)
            {
                httpContextAccessor.HttpContext?.Response.Headers.Append(
                    HeaderNames.ETag, $"\"{Convert.ToBase64String(entity.RowVersion)}\"");
            }
        }

        return dto;
    }
}
