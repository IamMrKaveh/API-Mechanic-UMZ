using Application.Order.Contracts;
using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

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

    public async Task<OrderStatusDto?> GetByIdAsync(
        OrderStatusId orderStatusId,
        CancellationToken ct = default)
    {
        return await context.OrderStatuses
            .AsNoTracking()
            .Where(s => s.Id == orderStatusId)
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
            .FirstOrDefaultAsync(ct);
    }
}