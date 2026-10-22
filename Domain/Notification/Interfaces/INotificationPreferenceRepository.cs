using Domain.Notification.Aggregates;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Notification.Interfaces;

public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetByUserIdAsync(
        UserId userId,
        CancellationToken ct = default);

    Task AddAsync(
        NotificationPreference preference,
        CancellationToken ct = default);

    void Update(NotificationPreference preference);
}
