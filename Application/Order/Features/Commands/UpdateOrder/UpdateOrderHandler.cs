using Domain.Common.Exceptions;
using Domain.Order.Interfaces;
using Domain.Shipping.Interfaces;

namespace Application.Order.Features.Commands.UpdateOrder;

public class UpdateOrderHandler(
    IOrderRepository orderRepository,
    IShippingRepository shippingRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateOrderCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(UpdateOrderCommand request, CancellationToken ct)
    {
        var order = await orderRepository.FindByIdAsync(request.OrderId, ct);
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
                var shipping = await shippingRepository.GetByIdAsync(
                    request.Dto.ShippingId.Value, ct);

                if (shipping == null || !shipping.IsActive)
                    return ServiceResult.Failure("روش ارسال نامعتبر است.");

                var shippingCost = shipping.CalculateCost(order.TotalAmount);
                order.UpdateShipping(shipping.Id, shippingCost);
            }

            await orderRepository.Update(order);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("این سفارش توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را رفرش کنید.");
        }
    }
}