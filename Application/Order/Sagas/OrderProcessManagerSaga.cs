namespace Application.Order.Sagas;

/// <summary>
/// Saga با وضعیت پایا (Persistent State)
/// وضعیت Saga در دیتابیس ذخیره می‌شود تا در crash/restart از بین نرود
/// از IOrderProcessStateRepository برای ذخیره وضعیت استفاده می‌شود
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
    private readonly IOrderProcessStateRepository _processStateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaymentSettlementService _paymentSettlementService;
    private readonly ILogger<OrderProcessManagerSaga> _logger;

    public OrderProcessManagerSaga(
        IOrderRepository orderRepository,
        IInventoryService inventoryService,
        IPaymentTransactionRepository paymentRepository,
        IOrderProcessStateRepository processStateRepository,
        IUnitOfWork unitOfWork,
        PaymentSettlementService paymentSettlementService,
        ILogger<OrderProcessManagerSaga> logger)
    {
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
        _paymentRepository = paymentRepository;
        _processStateRepository = processStateRepository;
        _unitOfWork = unitOfWork;
        _paymentSettlementService = paymentSettlementService;
        _logger = logger;
    }

    

    public async Task Handle(OrderCreatedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "[Saga] OrderCreated: OrderId={OrderId} → Starting inventory reservation",
            notification.OrderId);

        
        var processState = OrderProcessState.Create(
            notification.OrderId,
            correlationId: notification.OrderId.ToString());

        await _processStateRepository.AddAsync(processState, ct);
        processState.TransitionTo(OrderProcessState.Steps.InventoryReserving);
        await _unitOfWork.SaveChangesAsync(ct);

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            _logger.LogError("[Saga] Order {OrderId} not found after creation.", notification.OrderId);
            processState.MarkFailed("Order not found after creation.");
            await _unitOfWork.SaveChangesAsync(ct);
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

                processState.MarkCompensating();
                await _unitOfWork.SaveChangesAsync(ct);

                await CompensateOrderAsync(order, $"موجودی کافی نیست: {result.Error}", processState, ct);
                return;
            }
        }

        processState.TransitionTo(OrderProcessState.Steps.InventoryReserved);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[Saga] Inventory reserved successfully for Order {OrderId}", order.Id);
    }

    

    public async Task Handle(PaymentSucceededEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "[Saga] PaymentSucceeded: OrderId={OrderId}, RefId={RefId}",
            notification.OrderId, notification.RefId);

        
        var processState = await _processStateRepository.GetByOrderIdAsync(notification.OrderId, ct);
        if (processState is null)
        {
            _logger.LogWarning("[Saga] No process state found for Order {OrderId}. Creating one.", notification.OrderId);
            processState = OrderProcessState.Create(notification.OrderId);
            await _processStateRepository.AddAsync(processState, ct);
        }

        var order = await _orderRepository.GetByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            _logger.LogError("[Saga] Order {OrderId} not found after payment success.", notification.OrderId);
            processState.MarkFailed("Order not found after payment success.");
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var settlementResult = _paymentSettlementService.ProcessPaymentSuccess(
            order,
            notification.RefId,
            notification.CardPan);

        if (!settlementResult.IsSuccess)
        {
            _logger.LogError(
                "[Saga] Settlement failed for Order {OrderId}: {Error}",
                order.Id, settlementResult.Error);
            processState.MarkFailed($"Settlement failed: {settlementResult.Error}");
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        if (settlementResult.IsIdempotent)
        {
            _logger.LogInformation("[Saga] Order {OrderId} already processed (idempotent).", order.Id);
            processState.MarkCompleted();
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        await _orderRepository.UpdateAsync(order, ct);
        processState.TransitionTo(OrderProcessState.Steps.PaymentSucceeded);
        processState.MarkCompleted();
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("[Saga] Order {OrderId} settled: Paid + Processing", order.Id);
    }

    

    public async Task Handle(PaymentFailedEvent notification, CancellationToken ct)
    {
        _logger.LogWarning(
            "[Saga] PaymentFailed: OrderId={OrderId}, Reason={Reason}",
            notification.OrderId, notification.Reason);

        var processState = await _processStateRepository.GetByOrderIdAsync(notification.OrderId, ct);
        if (processState is not null)
        {
            processState.TransitionTo(OrderProcessState.Steps.PaymentPending);
            processState.IncrementRetry();
            await _unitOfWork.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "[Saga] Order {OrderId} payment failed. Inventory stays reserved for retry window.",
            notification.OrderId);
    }

    

    public async Task Handle(OrderCancelledEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "[Saga] OrderCancelled: OrderId={OrderId}, Reason={Reason}",
            notification.OrderId, notification.Reason);

        var processState = await _processStateRepository.GetByOrderIdAsync(notification.OrderId, ct);
        if (processState is not null)
        {
            processState.MarkCompensating();
            await _unitOfWork.SaveChangesAsync(ct);
        }

        await ReleaseInventoryForOrderAsync(notification.OrderId, "لغو سفارش", processState, ct);
    }

    

    public async Task Handle(OrderExpiredEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "[Saga] OrderExpired: OrderId={OrderId}", notification.OrderId);

        var processState = await _processStateRepository.GetByOrderIdAsync(notification.OrderId, ct);
        if (processState is not null)
        {
            processState.MarkCompensating();
            await _unitOfWork.SaveChangesAsync(ct);
        }

        await ReleaseInventoryForOrderAsync(notification.OrderId, "انقضای سفارش", processState, ct);
    }

    

    private async Task CompensateOrderAsync(
        Domain.Order.Order order,
        string reason,
        OrderProcessState? processState,
        CancellationToken ct)
    {
        try
        {
            var referenceNumber = $"ORDER-{order.Id}";
            await _inventoryService.RollbackReservationsAsync(referenceNumber, ct);

            order.Cancel(0, reason);
            await _orderRepository.UpdateAsync(order, ct);

            processState?.MarkCompensated();
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("[Saga] Order {OrderId} compensated: {Reason}", order.Id, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Saga] Compensation failed for Order {OrderId}", order.Id);
            processState?.MarkFailed($"Compensation failed: {ex.Message}");
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    private async Task ReleaseInventoryForOrderAsync(
        int orderId,
        string reason,
        OrderProcessState? processState,
        CancellationToken ct)
    {
        try
        {
            var referenceNumber = $"ORDER-{orderId}";
            var result = await _inventoryService.RollbackReservationsAsync(referenceNumber, ct);

            if (result.IsSucceed)
            {
                _logger.LogInformation(
                    "[Saga] Inventory released for Order {OrderId}: {Reason}", orderId, reason);
                processState?.MarkCompensated();
            }
            else
            {
                _logger.LogWarning(
                    "[Saga] Inventory release partially failed for Order {OrderId}: {Error}",
                    orderId, result.Error);
                processState?.MarkFailed($"Inventory release failed: {result.Error}");
            }

            if (processState is not null)
                await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Saga] Failed to release inventory for Order {OrderId}", orderId);
            processState?.MarkFailed($"Exception: {ex.Message}");
            if (processState is not null)
                await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}