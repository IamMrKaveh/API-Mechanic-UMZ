using Domain.Notification.Interfaces;
using Domain.Notification.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Notification.Services;

public sealed class NotificationService(
    INotificationRepository notificationRepository,
    IAuditService auditService,
    IUnitOfWork unitOfWork,
    DBContext context) : INotificationService
{
    public async Task CreateNotificationAsync(
        UserId userId,
        string title,
        string message,
        string typeName,
        string? actionUrl = null,
        Guid? referenceId = null,
        string? referenceType = null,
        CancellationToken ct = default)
    {
        var notificationType = NotificationType.FromString(typeName);
        var notification = Domain.Notification.Aggregates.Notification.Create(
            NotificationId.NewId(),
            userId,
            notificationType);

        await notificationRepository.AddAsync(notification, ct);

        context.Entry(notification).Property("Title").CurrentValue = title;
        context.Entry(notification).Property("Message").CurrentValue = message;
        context.Entry(notification).Property("Type").CurrentValue = typeName;
        context.Entry(notification).Property("ActionUrl").CurrentValue = actionUrl;
        context.Entry(notification).Property("RelatedEntityType").CurrentValue = referenceType;
        context.Entry(notification).Property("RelatedEntityId").CurrentValue = referenceId;

        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogSystemEventAsync(
            "NotificationCreated",
            $"Notification '{typeName}' created for user {userId.Value}", ct);
    }

    public async Task MarkAsReadAsync(
        NotificationId notificationId,
        UserId userId,
        CancellationToken ct = default)
    {
        var notification = await notificationRepository.GetByIdAsync(notificationId, ct);
        if (notification is null) return;

        notification.EnsureUserAccess(userId);
        notification.MarkAsRead();
        notificationRepository.Update(notification);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(
        UserId userId,
        CancellationToken ct = default)
    {
        var unread = await notificationRepository.GetUnreadByUserIdAsync(userId, ct);
        foreach (var n in unread)
            n.MarkAsRead();

        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(
        NotificationId notificationId,
        UserId userId,
        CancellationToken ct = default)
    {
        var notification = await notificationRepository.GetByIdAsync(notificationId, ct);
        if (notification is null) return;

        notification.EnsureUserAccess(userId);
        notificationRepository.Remove(notification);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task SendOrderStatusNotificationAsync(
        UserId userId,
        OrderId orderId,
        string oldStatusName,
        string newStatusName,
        CancellationToken ct = default)
    {
        var typeName = newStatusName switch
        {
            "Paid" => "OrderPaid",
            "Shipped" => "OrderShipped",
            "Delivered" => "OrderDelivered",
            "Cancelled" => "OrderCancelled",
            _ => "OrderCreated"
        };

        await CreateNotificationAsync(
            userId,
            "وضعیت سفارش تغییر کرد",
            $"وضعیت سفارش از '{oldStatusName}' به '{newStatusName}' تغییر یافت.",
            typeName,
            $"/dashboard/orders/{orderId.Value}",
            orderId.Value,
            "Order",
            ct);
    }
}