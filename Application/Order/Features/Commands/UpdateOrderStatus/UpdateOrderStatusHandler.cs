using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.UpdateOrderStatus;

public class UpdateOrderStatusHandler(
    IOrderRepository orderRepository,
    INotificationService notificationService)
    : ICommandHandler<UpdateOrderStatusCommand>
{
    public async Task<ServiceResult> Handle(
        UpdateOrderStatusCommand request,
        CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        var rowVersion = !string.IsNullOrEmpty(request.RowVersion)
            ? Convert.FromBase64String(request.RowVersion)
            : null;

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
                case "Pending": order.MoveToPending(); break;
                case "Processing": order.StartProcessing(); break;
                case "Shipped": order.MarkAsShipped(); break;
                case "Delivered": order.MarkAsDelivered(); break;
                case "Returned": order.MarkAsReturned(); break;
                case "Refunded": order.Refund(); break;
                case "Cancelled":
                    return ServiceResult.Validation("برای لغو سفارش از مسیر اختصاصی لغو استفاده کنید.");

                default:
                    return ServiceResult.Forbidden($"تغییر مستقیم به وضعیت '{newStatus.DisplayName}' مجاز نیست.");
            }
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        orderRepository.Update(order, rowVersion);

        await notificationService.SendOrderStatusNotificationAsync(
            order.UserId, order.Id, oldStatusName, newStatus.DisplayName, ct);

        return ServiceResult.Success();
    }
}
