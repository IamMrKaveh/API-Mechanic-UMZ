namespace Application.Payment.EventHandlers;

/// <summary>
/// پس از پرداخت موفق، رزرو موجودی سفارش را Commit می‌کند.
/// از جاسازی مستقیم در VerifyPaymentHandler جدا شده تا Single Responsibility رعایت شود.
/// </summary>
public class PaymentSucceededInventoryCommitEventHandler
    : INotificationHandler<PaymentSucceededEvent>
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<PaymentSucceededInventoryCommitEventHandler> _logger;

    public PaymentSucceededInventoryCommitEventHandler(
        IInventoryService inventoryService,
        ILogger<PaymentSucceededInventoryCommitEventHandler> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task Handle(PaymentSucceededEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Committing inventory reservations for Order {OrderId} after successful payment.",
            notification.OrderId);

        var result = await _inventoryService.CommitStockForOrderAsync(
            notification.OrderId, cancellationToken);

        if (result.IsFailed)
        {
            // لاگ کردن خطا - در صورت fail شدن، Outbox/Retry policy باید مجدداً تلاش کند
            _logger.LogError(
                "Failed to commit inventory for Order {OrderId}. Error: {Error}. Manual reconciliation may be required.",
                notification.OrderId, result.Error);

            // TODO: در صورت نیاز Outbox pattern یا Dead Letter Queue اضافه شود
            // در حال حاضر فقط لاگ می‌کنیم تا پرداخت fail نشود
        }
        else
        {
            _logger.LogInformation(
                "Successfully committed inventory reservations for Order {OrderId}.",
                notification.OrderId);
        }
    }
}