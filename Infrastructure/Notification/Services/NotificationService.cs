namespace Infrastructure.Notification.Services;

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

    public async Task CreateNotificationAsync(
        int userId,
        string title,
        string message,
        string type,
        string? actionUrl = null,
        int? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken ct = default)
    {
        try
        {
            var notification = Domain.Notification.Notification.Create(
                userId,
                title,
                message,
                type,
                actionUrl,
                relatedEntityId,
                relatedEntityType);

            await _context.Notifications.AddAsync(notification, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Notification created for user {UserId}: {Title} (Type: {Type})",
                userId, title, type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for user {UserId}: {Title}", userId, title);
        }
    }

    public async Task SendOrderStatusNotificationAsync(
        int userId,
        int orderId,
        string oldStatus,
        string newStatus,
        CancellationToken ct = default)
    {
        var notificationType = newStatus switch
        {
            "Paid" => "OrderPaid",
            "Shipped" => "OrderShipped",
            "Delivered" => "OrderDelivered",
            "Cancelled" => "OrderCancelled",
            _ => "OrderStatus"
        };

        await CreateNotificationAsync(
            userId,
            "تغییر وضعیت سفارش",
            $"وضعیت سفارش #{orderId} از {oldStatus} به {newStatus} تغییر کرد.",
            notificationType,
            $"/dashboard/orders/{orderId}",
            orderId,
            "Order",
            ct);
    }

    public async Task SendPaymentNotificationAsync(
        int userId,
        int orderId,
        bool isSuccess,
        string? refId = null,
        CancellationToken ct = default)
    {
        if (isSuccess)
        {
            await CreateNotificationAsync(
                userId,
                "پرداخت موفق",
                $"پرداخت سفارش #{orderId} با موفقیت انجام شد. کد پیگیری: {refId}",
                "PaymentSuccess",
                $"/dashboard/orders/{orderId}",
                orderId,
                "Order",
                ct);
        }
        else
        {
            await CreateNotificationAsync(
                userId,
                "پرداخت ناموفق",
                $"پرداخت سفارش #{orderId} ناموفق بود. لطفاً مجدداً تلاش کنید.",
                "PaymentFailed",
                "/checkout",
                orderId,
                "Order",
                ct);
        }
    }

    public async Task SendLowStockNotificationAsync(
        int productId,
        string productName,
        int currentStock,
        CancellationToken ct = default)
    {
        var admins = await _context.Users
            .Where(u => u.IsAdmin && !u.IsDeleted)
            .Select(u => u.Id)
            .ToListAsync(ct);

        foreach (var adminId in admins)
        {
            await CreateNotificationAsync(
                adminId,
                "هشدار موجودی کم",
                $"موجودی محصول «{productName}» به {currentStock} عدد رسیده است.",
                "StockAlert",
                $"/admin/products/{productId}",
                productId,
                "Product",
                ct);
        }
    }
}