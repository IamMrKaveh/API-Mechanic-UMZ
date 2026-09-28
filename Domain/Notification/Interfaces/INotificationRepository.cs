using Domain.Common.Interfaces;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Notification.Interfaces;

public interface INotificationRepository : IRepository<Aggregates.Notification, NotificationId>
{
    Task<IReadOnlyList<Aggregates.Notification>> GetUnreadByUserIdAsync(
        UserId userId,
        CancellationToken ct = default);

    void Remove(Aggregates.Notification notification);
}