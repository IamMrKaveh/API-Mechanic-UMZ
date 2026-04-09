using Domain.Common.Exceptions;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.UpdateOrderStatus;

public class UpdateOrderStatusHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    IAuditService auditService,
    OrderDomainService orderDomainService,
    ILogger<UpdateOrderStatusHandler> logger) : IRequestHandler<UpdateOrderStatusCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IAuditService _auditService = auditService;
    private readonly OrderDomainService _orderDomainService = orderDomainService;
    private readonly ILogger<UpdateOrderStatusHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        UpdateOrderStatusCommand request,
        CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, ct);
        if (order == null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (!string.IsNullOrEmpty(request.RowVersion))
            _orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.RowVersion));

        OrderStatusValue newStatus;
        try
        {
            newStatus = OrderStatusValue.From(request.NewStatus);
        }
        catch (DomainException)
        {
            return ServiceResult.Unexpected("وضعیت سفارش نامعتبر است.");
        }

        var validation = _orderDomainService.ValidateStatusTransition(order, newStatus);
        if (!validation.IsValid)
            return ServiceResult.Validation(validation.Error!);

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
                return ServiceResult.Forbidden($"تغییر مستقیم به وضعیت '{newStatus.DisplayName}' از این مسیر مجاز نیست.");
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

            return ServiceResult.Conflict("این سفارش توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را رفرش کنید.");
        }
    }
}