using Domain.Notification.Interfaces;

namespace Infrastructure.Notification.Repositories;

public class NotificationRepository(DBContext context) : INotificationRepository
{
    private readonly DBContext _context = context;

    public async Task<int> MarkAllAsReadByUserIdAsync(
        int userId,
        CancellationToken ct = default)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
    }

    public async Task<int> DeleteOldNotificationsAsync(
        DateTime olderThan,
        CancellationToken ct = default)
    {
        return await _context.Notifications
            .Where(n => n.CreatedAt < olderThan && n.IsRead)
            .ExecuteDeleteAsync(ct);
    }

    public async Task AddAsync(
        Domain.Notification.Notification notification,
        CancellationToken ct = default)
    {
        await _context.Notifications.AddAsync(notification, ct);
    }

    public void Update(Domain.Notification.Notification notification)
    {
        _context.Notifications.Update(notification);
    }
}