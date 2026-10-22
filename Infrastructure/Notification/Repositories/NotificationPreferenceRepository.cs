using Domain.Notification.Aggregates;
using Domain.Notification.Interfaces;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Repositories;

namespace Infrastructure.Notification.Repositories;

public sealed class NotificationPreferenceRepository(DBContext context)
    : RepositoryBase<NotificationPreference, NotificationPreferenceId>(context),
      INotificationPreferenceRepository
{
    public Task<NotificationPreference?> GetByUserIdAsync(
        UserId userId,
        CancellationToken ct = default)
        => Context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

    void INotificationPreferenceRepository.Update(NotificationPreference preference)
        => Context.NotificationPreferences.Update(preference);
}
