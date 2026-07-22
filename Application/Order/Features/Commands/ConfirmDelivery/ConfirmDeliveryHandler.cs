using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.ConfirmDelivery;

public class ConfirmDeliveryHandler(
    IOrderRepository orderRepository,
    ICurrentUserService currentUser)
    : ICommandHandler<ConfirmDeliveryCommand>
{
    public async Task<ServiceResult> Handle(ConfirmDeliveryCommand request, CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            return ServiceResult.Unauthorized("کاربر احراز هویت نشده است.");

        var orderId = OrderId.From(request.OrderId);
        var userId = UserId.From(currentUser.UserId.Value);

        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

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

        try
        {
            order.MarkAsDelivered();
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
