using Domain.Notification.Aggregates;
using Domain.Notification.Interfaces;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Notification.Repositories;

public sealed class NotificationRepository(DBContext context) : INotificationRepository
{
    public async Task<Domain.Notification.Aggregates.Notification?> GetByIdAsync(
        NotificationId id,
        CancellationToken ct = default)
    {
        return await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, ct);
    }

    public async Task<IReadOnlyList<Domain.Notification.Aggregates.Notification>> GetByUserIdAsync(
        UserId userId,
        bool unreadOnly,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Notifications.Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var results = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<int> GetUnreadCountAsync(UserId userId, CancellationToken ct = default)
    {
        return await context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task AddAsync(Domain.Notification.Aggregates.Notification notification, CancellationToken ct = default)
    {
        await context.Notifications.AddAsync(notification, ct);
    }

    public void Update(Domain.Notification.Aggregates.Notification notification)
    {
        context.Notifications.Update(notification);
    }
}