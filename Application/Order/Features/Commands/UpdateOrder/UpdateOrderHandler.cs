using Domain.Common.Exceptions;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;

namespace Application.Order.Features.Commands.UpdateOrder;

public class UpdateOrderHandler(
    IOrderRepository orderRepository,
    IShippingRepository shippingRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateOrderCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(UpdateOrderCommand request, CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (!string.IsNullOrEmpty(request.Dto.RowVersion))
            orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.Dto.RowVersion));

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

            orderRepository.Update(order);
            await unitOfWork.SaveChangesAsync(ct);

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