using Application.Features.Orders.Commands.UpdateOrder;

namespace Application.Order.Features.Commands.UpdateOrder;

public class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IShippingRepository _shippingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateOrderHandler> _logger;

    public UpdateOrderHandler(
        IOrderRepository orderRepository,
        IShippingRepository shippingRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _shippingRepository = shippingRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(UpdateOrderCommand request, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, ct);
        if (order == null)
            return ServiceResult.Failure("سفارش یافت نشد.", 404);

        if (!string.IsNullOrEmpty(request.Dto.RowVersion))
            _orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.Dto.RowVersion));

        if (!order.CanBeModified())
            return ServiceResult.Failure("این سفارش قابل ویرایش نیست.", 400);

        try
        {
            if (request.Dto.ShippingId.HasValue)
            {
                var shipping = await _shippingRepository.GetByIdAsync(
                    request.Dto.ShippingId.Value, ct);

                if (shipping == null || !shipping.IsActive)
                    return ServiceResult.Failure("روش ارسال نامعتبر است.", 400);

                var shippingCost = shipping.CalculateCost(order.TotalAmount);
                order.UpdateShipping(shipping.Id, shippingCost);
            }

            await _orderRepository.UpdateAsync(order, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message, 400);
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure("این سفارش توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را رفرش کنید.");
        }
    }
}