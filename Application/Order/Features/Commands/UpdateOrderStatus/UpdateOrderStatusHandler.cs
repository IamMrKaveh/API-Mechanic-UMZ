using Domain.Common.Exceptions;
using Domain.Common.ValueObjects;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.UpdateOrderStatus;

public class UpdateOrderStatusHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    IAuditService auditService) : IRequestHandler<UpdateOrderStatusCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        UpdateOrderStatusCommand request,
        CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var order = await orderRepository.FindWithItemsByIdAsync(orderId, ct);
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

        if (!order.Status.CanTransitionTo(newStatus))
            return ServiceResult.Validation($"انتقال به وضعیت '{newStatus.DisplayName}' مجاز نیست.");

        var oldStatusName = order.Status.DisplayName;

        try
        {
            switch (newStatus.Value)
            {
                case "Processing":
                    order.StartProcessing();
                    break;

                case "Shipped":
                    order.MarkAsShipped();
                    break;

                case "Delivered":
                    order.MarkAsDelivered();
                    break;

                default:
                    return ServiceResult.Forbidden($"تغییر مستقیم به وضعیت '{newStatus.DisplayName}' مجاز نیست.");
            }
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        orderRepository.Update(order);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);

            await notificationService.SendOrderStatusNotificationAsync(
                order.UserId,
                order.Id,
                oldStatusName,
                newStatus.DisplayName,
                ct);

            await auditService.LogOrderEventAsync(
                order.Id,
                "UpdateOrderStatus",
                IpAddress.Unknown,
                UserId.From(request.UpdatedByUserId),
                $"وضعیت سفارش از {oldStatusName} به {newStatus.DisplayName} تغییر کرد.",
                ct);

            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("این سفارش توسط کاربر دیگری تغییر کرده است.");
        }
    }
}