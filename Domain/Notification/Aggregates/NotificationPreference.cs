using Domain.Notification.Enums;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Notification.Aggregates;

/// <summary>
/// The notification preferences of a user: which notification types the user
/// does not want to receive.
///
/// A user without preferences receives everything, so the aggregate only
/// records what was explicitly switched off. That keeps the default behaviour
/// of the notification pipeline unchanged for every existing account.
/// </summary>
public sealed class NotificationPreference : AggregateRoot<NotificationPreferenceId>
{
    private List<string> _disabledTypeNames = [];

    public UserId UserId { get; private init; } = default!;

    /// <summary>The names of the notification types the user switched off.</summary>
    public IReadOnlyCollection<string> DisabledTypeNames => _disabledTypeNames.AsReadOnly();

    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    private NotificationPreference()
    { }

    private NotificationPreference(NotificationPreferenceId id, UserId userId, DateTime now) : base(id)
    {
        UserId = userId;
        CreatedAt = now;
    }

    public static NotificationPreference Create(NotificationPreferenceId id, UserId userId, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(userId);

        return new NotificationPreference(id, userId, now);
    }

    /// <summary>True when the user did not switch this notification type off.</summary>
    public bool IsEnabled(NotificationType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return !_disabledTypeNames.Contains(type.Value, StringComparer.OrdinalIgnoreCase);
    }

    public void Disable(NotificationType type, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (_disabledTypeNames.Contains(type.Value, StringComparer.OrdinalIgnoreCase))
            return;

        _disabledTypeNames.Add(type.Value);
        UpdatedAt = now;
    }

    public void Enable(NotificationType type, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(type);

        var existing = _disabledTypeNames.FirstOrDefault(
            name => string.Equals(name, type.Value, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
            return;

        _disabledTypeNames.Remove(existing);
        UpdatedAt = now;
    }

    public void DisableCategory(NotificationCategory category, DateTime now)
    {
        foreach (var type in NotificationType.GetAll().Where(t => t.Category == category))
            Disable(type, now);
    }

    public void EnableCategory(NotificationCategory category, DateTime now)
    {
        foreach (var type in NotificationType.GetAll().Where(t => t.Category == category))
            Enable(type, now);
    }

    /// <summary>Replaces the whole set of disabled types with the given ones.</summary>
    public void ReplaceDisabledTypes(IEnumerable<NotificationType> types, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(types);

        _disabledTypeNames.Clear();

        foreach (var type in types
            .GroupBy(t => t.Value, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First()))
        {
            _disabledTypeNames.Add(type.Value);
        }

        UpdatedAt = now;
    }
}
