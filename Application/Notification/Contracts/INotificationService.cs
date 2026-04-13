using Domain.Notification.ValueObjects;
using Domain.Order.ValueObjects;
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

    Task MarkAsReadAsync(
        NotificationId notificationId,
        UserId userId,
        CancellationToken ct = default);

    Task MarkAllAsReadAsync(
        UserId userId,
        CancellationToken ct = default);

    Task DeleteAsync(
        NotificationId notificationId,
        UserId userId,
        CancellationToken ct = default);

    Task SendOrderStatusNotificationAsync(
        UserId userId,
        OrderId orderId,
        string oldStatusName,
        string newStatusName,
        CancellationToken ct = default);
}