namespace Application.Order.Features.Commands.MarkOrderAsShipped;

public class MarkOrderAsShippedHandler : IRequestHandler<MarkOrderAsShippedCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly ILogger<MarkOrderAsShippedHandler> _logger;

    public MarkOrderAsShippedHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IAuditService auditService,
        ILogger<MarkOrderAsShippedHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        MarkOrderAsShippedCommand request,
        CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, ct);
        if (order == null)
            return ServiceResult.Failure("سفارش یافت نشد.", 404);

        if (!string.IsNullOrEmpty(request.RowVersion))
            _orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.RowVersion));

        var oldStatusName = order.Status.DisplayName;

        try
        {
            order.Ship();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message, 400);
        }

        await _orderRepository.UpdateAsync(order, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);

            await _notificationService.SendOrderStatusNotificationAsync(
                order.UserId,
                order.Id,
                oldStatusName,
                OrderStatusValue.Shipped.DisplayName);

            await _auditService.LogOrderEventAsync(
                order.Id,
                "ShipOrder",
                request.UpdatedByUserId,
                $"سفارش ارسال شد.");

            _logger.LogInformation("Order {OrderId} shipped", order.Id);

            return ServiceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Failure(
                "این سفارش توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را رفرش کنید.", 409);
        }
    }
}