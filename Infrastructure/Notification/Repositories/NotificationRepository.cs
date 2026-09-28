using Domain.Notification.Interfaces;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

using Infrastructure.Persistence.Repositories;

namespace Infrastructure.Notification.Repositories;

public sealed class NotificationRepository(DBContext context)
    : RepositoryBase<Domain.Notification.Aggregates.Notification, NotificationId>(context), INotificationRepository
{
    public async Task<IReadOnlyList<Domain.Notification.Aggregates.Notification>> GetUnreadByUserIdAsync(
        UserId userId,
        CancellationToken ct = default)
    {
        var results = await Context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public void Remove(Domain.Notification.Aggregates.Notification notification)
        => Context.Notifications.Remove(notification);
}