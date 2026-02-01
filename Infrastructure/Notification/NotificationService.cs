using Application.Common.Interfaces.Notification;
using Application.DTOs.Notification;
using Infrastructure.Persistence.Interface.Common;

namespace Infrastructure.Notification;

public class NotificationService : INotificationService
{
    private readonly LedkaContext _context;
    private readonly ILogger<NotificationService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(
        LedkaContext context,
        ILogger<NotificationService> logger,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task CreateNotificationAsync(int userId, string title, string message, string type, string? actionUrl = null, int? relatedEntityId = null, string? relatedEntityType = null)
    {
        var notification = new Domain.Notification.Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            ActionUrl = actionUrl,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        await _context.Notifications.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Notification created for user {UserId}: {Title}", userId, title);
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly = false, int page = 1, int pageSize = 20)
    {
        var query = _context.Notifications.Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                ActionUrl = n.ActionUrl,
                RelatedEntityId = n.RelatedEntityId,
                RelatedEntityType = n.RelatedEntityType,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt
            })
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null || notification.IsRead)
            return false;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow));
    }

    public async Task DeleteNotificationAsync(int notificationId, int userId)
    {
        await _context.Notifications
            .Where(n => n.Id == notificationId && n.UserId == userId)
            .ExecuteDeleteAsync();
    }

    public async Task SendOrderStatusNotificationAsync(int userId, int orderId, string oldStatus, string newStatus)
    {
        await CreateNotificationAsync(
            userId,
            "تغییر وضعیت سفارش",
            $"وضعیت سفارش #{orderId} از {oldStatus} به {newStatus} تغییر کرد.",
            "OrderStatus",
            $"/dashboard/orders/{orderId}",
            orderId,
            "Order"
        );
    }

    public async Task SendPaymentNotificationAsync(int userId, int orderId, bool isSuccess, string? refId = null)
    {
        if (isSuccess)
        {
            await CreateNotificationAsync(
                userId,
                "پرداخت موفق",
                $"پرداخت سفارش #{orderId} با موفقیت انجام شد.  کد پیگیری: {refId}",
                "PaymentSuccess",
                $"/dashboard/orders/{orderId}",
                orderId,
                "Order"
            );
        }
        else
        {
            await CreateNotificationAsync(
                userId,
                "پرداخت ناموفق",
                $"پرداخت سفارش #{orderId} ناموفق بود. لطفا مجددا تلاش کنید.",
                "PaymentFailed",
                $"/checkout",
                orderId,
                "Order"
            );
        }
    }

    public async Task SendLowStockNotificationAsync(int productId, string productName, int currentStock)
    {
        var admins = await _context.Users.Where(u => u.IsAdmin && !u.IsDeleted).Select(u => u.Id).ToListAsync();

        foreach (var adminId in admins)
        {
            await CreateNotificationAsync(
                adminId,
                "هشدار موجودی کم",
                $"موجودی محصول '{productName}' به {currentStock} عدد رسیده است.",
                "LowStock",
                $"/admin/products/{productId}",
                productId,
                "Product"
            );
        }
    }
}