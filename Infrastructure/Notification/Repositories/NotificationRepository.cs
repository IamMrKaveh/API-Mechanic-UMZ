using Domain.Notification.Interfaces;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

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
        CancellationToken ct = default)
    {
        var results = await context.Notifications
            .Where(n => n.UserId.Value == userId.Value)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<Domain.Notification.Aggregates.Notification>> GetUnreadByUserIdAsync(
        UserId userId,
        CancellationToken ct = default)
    {
        var results = await context.Notifications
            .Where(n => n.UserId.Value == userId.Value && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<Domain.Notification.Aggregates.Notification>> GetReadNotificationsOlderThanAsync(
        DateTime cutoff,
        CancellationToken ct = default)
    {
        var results = await context.Notifications
            .Where(n => n.IsRead && n.CreatedAt < cutoff)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task AddAsync(
        Domain.Notification.Aggregates.Notification notification,
        CancellationToken ct = default)
    {
        await context.Notifications.AddAsync(notification, ct);
    }

    public void Update(Domain.Notification.Aggregates.Notification notification)
    {
        context.Notifications.Update(notification);
    }

    public void Remove(Domain.Notification.Aggregates.Notification notification)
    {
        context.Notifications.Remove(notification);
    }
}