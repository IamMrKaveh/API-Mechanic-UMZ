using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.RequestReturn;

public class RequestReturnHandler(
    IOrderRepository orderRepository,
    INotificationService notificationService)
    : ICommandHandler<RequestReturnCommand>
{
    public async Task<ServiceResult> Handle(
        RequestReturnCommand request,
        CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (order.UserId != UserId.From(request.UserId))
            return ServiceResult.Unauthorized("شما مجاز به درخواست بازگشت این سفارش نیستید.");

        var rowVersion = !string.IsNullOrEmpty(request.RowVersion)
            ? Convert.FromBase64String(request.RowVersion)
            : null;

        var oldStatusName = order.Status.DisplayName;

        try
        {
            order.MarkAsReturned();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        orderRepository.Update(order, rowVersion);

        await notificationService.SendOrderStatusNotificationAsync(
            order.UserId,
            order.Id,
            oldStatusName,
            OrderStatusValue.Returned.DisplayName,
            ct);

        return ServiceResult.Success();
    }
}
