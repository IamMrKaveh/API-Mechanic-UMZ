using Application.Common.Exceptions;
using Application.Common.Results;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Order.Interfaces;
using Domain.Shipping.Interfaces;

namespace Application.Order.Features.Commands.UpdateOrder;

public class UpdateOrderHandler(
    IOrderRepository orderRepository,
    IShippingRepository shippingRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateOrderHandler> logger) : IRequestHandler<UpdateOrderCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IShippingRepository _shippingRepository = shippingRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<UpdateOrderHandler> _logger = logger;

    public async Task<ServiceResult> Handle(UpdateOrderCommand request, CancellationToken ct)
    {
        var order = await _orderRepository.FindByIdAsync(request.OrderId, ct);
        if (order == null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (!string.IsNullOrEmpty(request.Dto.RowVersion))
            _orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.Dto.RowVersion));

        if (!order.CanBeModified())
            return ServiceResult.Forbidden("این سفارش قابل ویرایش نیست.");

        try
        {
            if (request.Dto.ShippingId.HasValue)
            {
                var shipping = await _shippingRepository.GetByIdAsync(
                    request.Dto.ShippingId.Value, ct);

                if (shipping == null || !shipping.IsActive)
                    return ServiceResult.Unexpected("روش ارسال نامعتبر است.");

                var shippingCost = shipping.CalculateCost(order.TotalAmount);
                order.UpdateShipping(shipping.Id, shippingCost);
            }

            await _orderRepository.UpdateAsync(order, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("این سفارش توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را رفرش کنید.");
        }
    }
}