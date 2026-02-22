namespace Application.Order.Features.Commands.UpdateOrderStatus;

public class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly OrderDomainService _orderDomainService;
    private readonly ILogger<UpdateOrderStatusHandler> _logger;

    public UpdateOrderStatusHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IAuditService auditService,
        OrderDomainService orderDomainService,
        ILogger<UpdateOrderStatusHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _auditService = auditService;
        _orderDomainService = orderDomainService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        UpdateOrderStatusCommand request,
        CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, ct);
        if (order == null)
            return ServiceResult.Failure("سفارش یافت نشد.", 404);

        if (!string.IsNullOrEmpty(request.RowVersion))
            _orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.RowVersion));

        OrderStatusValue newStatus;
        try
        {
            newStatus = OrderStatusValue.FromString(request.NewStatus);
        }
        catch (DomainException)
        {
            return ServiceResult.Failure("وضعیت سفارش نامعتبر است.", 400);
        }

        var validation = _orderDomainService.ValidateStatusTransition(order, newStatus);
        if (!validation.IsValid)
            return ServiceResult.Failure(validation.Error!, 400);

        var oldStatusName = order.Status.DisplayName;

        switch (newStatus.Value)
        {
            case "Processing":
                order.StartProcessing();
                break;

            case "Shipped":
                order.Ship();
                break;

            case "Delivered":
                order.MarkAsDelivered();
                break;

            default:
                return ServiceResult.Failure($"تغییر مستقیم به وضعیت '{newStatus.DisplayName}' از این مسیر مجاز نیست.", 400);
        }

        await _orderRepository.UpdateAsync(order, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);

            await _notificationService.SendOrderStatusNotificationAsync(
                order.UserId,
                order.Id,
                oldStatusName,
                newStatus.DisplayName);

            await _auditService.LogOrderEventAsync(
                order.Id,
                "UpdateOrderStatus",
                request.UpdatedByUserId,
                $"وضعیت سفارش از {oldStatusName} به {newStatus.DisplayName} تغییر کرد.");

            _logger.LogInformation(
                "Order {OrderId} status updated from {OldStatus} to {NewStatus}",
                order.Id, oldStatusName, newStatus.DisplayName);

            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict updating order {OrderId} status",
                request.OrderId);

            return ServiceResult.Failure(
                "این سفارش توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را رفرش کنید.",
                409);
        }
    }
}