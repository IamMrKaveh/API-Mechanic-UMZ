using Domain.Common.Exceptions;
using Domain.Common.ValueObjects;
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
    public async Task<ServiceResult> Handle(
        UpdateOrderStatusCommand request,
        CancellationToken ct)
    {
        var order = await orderRepository.GetByIdWithItemsAsync(request.OrderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (!string.IsNullOrEmpty(request.RowVersion))
            orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.RowVersion));

        OrderStatusValue newStatus;
        try
        {
            newStatus = OrderStatusValue.From(request.NewStatus);
        }
        catch (DomainException)
        {
            return ServiceResult.Failure("وضعیت سفارش نامعتبر است.");
        }

        var validation = orderDomainService.ValidateStatusTransition(order, newStatus);
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

        await orderRepository.Update(order);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);

            await notificationService.SendOrderStatusNotificationAsync(
                order.UserId,
                order.Id,
                oldStatusName,
                newStatus.DisplayName);

            await auditService.LogOrderEventAsync(
                order.Id,
                "UpdateOrderStatus",
                IpAddress.Unknown,
                request.UpdatedByUserId,
                $"وضعیت سفارش از {oldStatusName} به {newStatus.DisplayName} تغییر کرد.");

            logger.LogInformation(
                "Order {OrderId} status updated from {OldStatus} to {NewStatus}",
                order.Id, oldStatusName, newStatus.DisplayName);

            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            logger.LogWarning(
                "Concurrency conflict updating order {OrderId} status",
                request.OrderId);

            return ServiceResult.Conflict("این سفارش توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را رفرش کنید.");
        }
    }
}