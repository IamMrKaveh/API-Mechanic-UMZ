using Domain.User.ValueObjects;

namespace Application.Notification.Contracts;

public interface INotificationService
{
    Task CreateNotificationAsync(
        UserId userId,
        string title,
        string message,
        string typeName,
        string? actionUrl = null,
        Guid? referenceId = null,
        string? referenceType = null,
        CancellationToken ct = default);

    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);

    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);

    Task DeleteAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
}