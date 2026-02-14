namespace Application.Payment.EventHandlers;

/// <summary>
/// وقتی پرداخت موفق شد، پیامک و نوتیفیکیشن ارسال می‌شود
/// </summary>
public sealed class PaymentSucceededNotificationEventHandler : INotificationHandler<Domain.Payment.Events.PaymentSucceededEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ISmsService _smsService;
    private readonly ILogger<PaymentSucceededNotificationEventHandler> _logger;

    public PaymentSucceededNotificationEventHandler(
        INotificationService notificationService,
        ISmsService smsService,
        ILogger<PaymentSucceededNotificationEventHandler> logger)
    {
        _notificationService = notificationService;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task Handle(Domain.Payment.Events.PaymentSucceededEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // نوتیفیکیشن درون‌برنامه‌ای
            await _notificationService.SendPaymentNotificationAsync(
                notification.UserId,
                notification.OrderId,
                isSuccess: true,
                Convert.ToString(notification.RefId),
                cancellationToken);

            _logger.LogInformation(
                "Payment success notification sent for order {OrderId}, RefId: {RefId}",
                notification.OrderId,
                notification.RefId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle PaymentSucceededEvent for order {OrderId}", notification.OrderId);
        }
    }
}