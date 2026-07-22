using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.RequestReturn;

public class RequestReturnHandler(
    IOrderRepository orderRepository,
    INotificationService notificationService,
    ICurrentUserService currentUser)
    : ICommandHandler<RequestReturnCommand>
{
    public async Task<ServiceResult> Handle(
        RequestReturnCommand request,
        CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            return ServiceResult.Unauthorized("کاربر احراز هویت نشده است.");

        var orderId = OrderId.From(request.OrderId);
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        var userId = UserId.From(currentUser.UserId.Value);

        if (!currentUser.IsAdmin && order.UserId != userId)
            return ServiceResult.Forbidden("دسترسی ممنوع.");

        byte[]? rowVersion = null;
        if (!string.IsNullOrEmpty(request.RowVersion))
        {
            try
            {
                rowVersion = Convert.FromBase64String(request.RowVersion);
            }
            catch (FormatException)
            {
                return ServiceResult.Validation("If-Match نامعتبر است.");
            }
        }

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
