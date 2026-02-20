using Application.Features.Orders.Commands.UpdateOrder;

namespace Application.Order.Features.Commands.UpdateOrder;

public class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IShippingRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateOrderHandler> _logger;

    public UpdateOrderHandler(
        IOrderRepository orderRepository,
        IShippingRepository shippingMethodRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _shippingMethodRepository = shippingMethodRepository;
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

        // Only modifiable orders (not paid, not cancelled, not deleted)
        if (!order.CanBeModified())
            return ServiceResult.Failure("این سفارش قابل ویرایش نیست.", 400);

        try
        {
            // Update shipping method through domain method
            if (request.Dto.ShippingMethodId.HasValue)
            {
                var shippingMethod = await _shippingMethodRepository.GetByIdAsync(
                    request.Dto.ShippingMethodId.Value, ct);

                if (shippingMethod == null || !shippingMethod.IsActive)
                    return ServiceResult.Failure("روش ارسال نامعتبر است.", 400);

                var shippingCost = shippingMethod.CalculateCost(order.TotalAmount);
                order.UpdateShippingMethod(shippingMethod.Id, shippingCost);
            }

            await _orderRepository.UpdateAsync(order, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message, 400);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Failure("این سفارش توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را رفرش کنید.");
        }
    }
}