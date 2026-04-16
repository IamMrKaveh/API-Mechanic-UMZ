using Application.Notification.Contracts;
using Application.Notification.Features.Shared;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Notification.QueryServices;

public sealed class NotificationQueryService(DBContext context) : INotificationQueryService
{
    public async Task<PaginatedResult<NotificationDto>> GetByUserIdAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId.Value == userId.Value);

        var totalItems = await query.CountAsync(ct);

        var dtos = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id.Value,
                UserId = n.UserId.Value,
                Title = EF.Property<string>(n, "Title"),
                Message = EF.Property<string>(n, "Message"),
                Type = EF.Property<string>(n, "Type"),
                ActionUrl = EF.Property<string?>(n, "ActionUrl"),
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(ct);

        return PaginatedResult<NotificationDto>.Create(dtos, totalItems, page, pageSize);
    }

    public async Task<int> GetUnreadCountAsync(
        UserId userId,
        CancellationToken ct = default)
    {
        return await context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId.Value == userId.Value && !n.IsRead, ct);
    }
}