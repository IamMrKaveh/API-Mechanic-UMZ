using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.CancelOrder;

public class CancelOrderHandler(
    IOrderRepository orderRepository,
    ICurrentUserService currentUser)
    : ICommandHandler<CancelOrderCommand>
{
    public async Task<ServiceResult> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            return ServiceResult.Unauthorized("کاربر احراز هویت نشده است.");

        var orderId = OrderId.From(request.OrderId);
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (!currentUser.IsAdmin && order.UserId != UserId.From(currentUser.UserId.Value))
            return ServiceResult.Forbidden("دسترسی ممنوع.");

        if (!order.CanBeCancelled())
            return ServiceResult.Failure("این سفارش قابل لغو نیست.");

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

        try
        {
            order.Cancel(request.Reason);
            orderRepository.Update(order, rowVersion);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("این سفارش توسط کاربر دیگری تغییر کرده است.");
        }
    }
}
