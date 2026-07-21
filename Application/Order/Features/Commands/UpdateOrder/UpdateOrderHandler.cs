using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;

namespace Application.Order.Features.Commands.UpdateOrder;

public class UpdateOrderHandler(
    IOrderRepository orderRepository,
    IShippingRepository shippingRepository)
    : ICommandHandler<UpdateOrderCommand>
{
    public async Task<ServiceResult> Handle(UpdateOrderCommand request, CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (!order.CanBeModified())
            return ServiceResult.Forbidden("این سفارش قابل ویرایش نیست.");

        try
        {
            if (request.Dto.ShippingId.HasValue)
            {
                var shippingId = ShippingId.From(request.Dto.ShippingId.Value);
                var shipping = await shippingRepository.GetByIdAsync(shippingId, ct);

                if (shipping is null || !shipping.IsActive)
                    return ServiceResult.Failure("روش ارسال نامعتبر است.");
            }

            var rowVersion = !string.IsNullOrEmpty(request.Dto.RowVersion)
                ? Convert.FromBase64String(request.Dto.RowVersion)
                : null;

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
