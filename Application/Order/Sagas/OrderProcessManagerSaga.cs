namespace Application.Order.Sagas;

/// <summary>
/// Saga / Process Manager برای هماهنگی چرخه حیات سفارش.
///
/// جریان موفق:
///   OrderCreated → [ReserveInventory] → OrderReserved
///   → [InitiatePayment] → OrderPending
///   → PaymentSucceeded → [ConfirmInventory] → [MarkOrderPaid]
///
/// جریان جبران (Compensation):
///   PaymentFailed → [RollbackInventory] → OrderFailed
///   OrderCancelled → [ReleaseInventory] → [RefundPayment]
///   OrderExpired → [ReleaseInventory] → OrderExpired
///
/// این کلاس به عنوان EventHandler ثبت می‌شود.
/// </summary>
public sealed class OrderProcessManagerSaga :
    INotificationHandler<OrderCreatedEvent>,
    INotificationHandler<PaymentSucceededEvent>,
    INotificationHandler<PaymentFailedEvent>,
    INotificationHandler<OrderCancelledEvent>,
    INotificationHandler<OrderExpiredEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IPaymentTransactionRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderProcessManagerSaga> _logger;

    public OrderProcessManagerSaga(
        IOrderRepository orderRepository,
        IInventoryService inventoryService,
        IPaymentTransactionRepository paymentRepository,
        IUnitOfWork unitOfWork,
        ILogger<OrderProcessManagerSaga> logger)
    {
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // ─── Step 1: رزرو موجودی پس از ایجاد سفارش ─────────────────

    public async Task Handle(OrderCreatedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "[Saga] OrderCreated: OrderId={OrderId} → Starting inventory reservation",
            notification.OrderId);

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            _logger.LogError("[Saga] Order {OrderId} not found after creation.", notification.OrderId);
            return;
        }

        var referenceNumber = $"ORDER-{order.Id}";

        foreach (var item in order.OrderItems)
        {
            var result = await _inventoryService.ReserveStockAsync(
                item.VariantId,
                item.Quantity,
                item.Id,
                order.UserId,
                referenceNumber,
                correlationId: order.IdempotencyKey,
                expiresAt: DateTime.UtcNow.AddMinutes(30),
                ct: ct);

            if (!result.IsSucceed)
            {
                _logger.LogWarning(
                    "[Saga] Inventory reservation failed for Variant {VariantId}: {Error}",
                    item.VariantId, result.Error);

                // اگر یک آیتم رزرو نشد، سفارش را لغو می‌کنیم
                await CompensateOrderAsync(order, $"موجودی کافی نیست: {result.Error}", ct);
                return;
            }
        }

        _logger.LogInformation(
            "[Saga] Inventory reserved successfully for Order {OrderId}", order.Id);
    }

    // ─── Step 2: تأیید موجودی و علامت‌گذاری پرداخت پس از PaymentSucceeded ──

    public async Task Handle(PaymentSucceededEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "[Saga] PaymentSucceeded: OrderId={OrderId}, RefId={RefId}",
            notification.OrderId, notification.RefId);

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            _logger.LogError("[Saga] Order {OrderId} not found after payment success.", notification.OrderId);
            return;
        }

        // تأیید نهایی موجودی (Reserved → Committed)
        var referenceNumber = $"ORDER-{order.Id}";
        var commitResult = await _inventoryService.CommitStockForOrderAsync(
                    order.Id,
                    ct);

        if (!commitResult.IsSucceed)
        {
            _logger.LogError(
                "[Saga] CRITICAL: Inventory commit failed for Order {OrderId} after payment: {Error}",
                order.Id, commitResult.Error);
            // این حالت نیاز به دخالت دستی دارد - ایجاد alert
        }

        // علامت‌گذاری سفارش به عنوان پرداخت شده
        order.MarkAsPaid(notification.RefId, notification.CardPan);
        order.StartProcessing();

        await _orderRepository.UpdateAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("[Saga] Order {OrderId} marked as Paid+Processing", order.Id);
    }

    // ─── Compensation: برگشت موجودی در صورت شکست پرداخت ─────────

    public async Task Handle(PaymentFailedEvent notification, CancellationToken ct)
    {
        _logger.LogWarning(
            "[Saga] PaymentFailed: OrderId={OrderId}, Reason={Reason}",
            notification.OrderId, notification.Reason);

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null) return;

        // اگر پرداخت‌های retry هنوز باقی است، موجودی را آزاد نمی‌کنیم
        // منطق retry توسط Payment Idempotency Service مدیریت می‌شود
        // اینجا فقط وضعیت سفارش را به Failed تغییر می‌دهیم

        _logger.LogInformation(
            "[Saga] Order {OrderId} payment failed. Inventory stays reserved for retry window.",
            order.Id);
    }

    // ─── Compensation: آزادسازی موجودی در صورت لغو سفارش ───────

    public async Task Handle(OrderCancelledEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "[Saga] OrderCancelled: OrderId={OrderId}, Reason={Reason}",
            notification.OrderId, notification.Reason);

        await ReleaseInventoryForOrderAsync(notification.OrderId, "لغو سفارش", ct);

        // اگر سفارش پرداخت شده بود، Refund آغاز می‌شود
        // این بخش توسط RefundPayment Command مدیریت می‌شود
    }

    // ─── Compensation: آزادسازی موجودی در صورت انقضای سفارش ─────

    public async Task Handle(OrderExpiredEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "[Saga] OrderExpired: OrderId={OrderId}", notification.OrderId);

        await ReleaseInventoryForOrderAsync(notification.OrderId, "انقضای سفارش", ct);
    }

    // ─── Private Helpers ──────────────────────────────────────────

    private async Task CompensateOrderAsync(Domain.Order.Order order, string reason, CancellationToken ct)
    {
        try
        {
            // آزادسازی موجودی‌های رزرو شده
            var referenceNumber = $"ORDER-{order.Id}";
            await _inventoryService.RollbackReservationsAsync(referenceNumber, ct);

            // لغو سفارش
            order.Cancel(0, reason); // 0 = سیستم
            await _orderRepository.UpdateAsync(order, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("[Saga] Order {OrderId} compensated: {Reason}", order.Id, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Saga] Compensation failed for Order {OrderId}", order.Id);
        }
    }

    private async Task ReleaseInventoryForOrderAsync(int orderId, string reason, CancellationToken ct)
    {
        try
        {
            var referenceNumber = $"ORDER-{orderId}";
            var result = await _inventoryService.RollbackReservationsAsync(referenceNumber, ct);

            if (result.IsSucceed)
                _logger.LogInformation(
                    "[Saga] Inventory released for Order {OrderId}: {Reason}", orderId, reason);
            else
                _logger.LogWarning(
                    "[Saga] Inventory release partially failed for Order {OrderId}: {Error}",
                    orderId, result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Saga] Failed to release inventory for Order {OrderId}", orderId);
        }
    }
}