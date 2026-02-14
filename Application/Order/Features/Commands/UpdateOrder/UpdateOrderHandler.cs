using Application.Features.Orders.Commands.UpdateOrder;

namespace Application.Order.Features.Commands.UpdateOrder;

public class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateOrderHandler> _logger;

    public UpdateOrderHandler(
        IOrderRepository orderRepository,
        IShippingMethodRepository shippingMethodRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, cancellationToken);
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
                    request.Dto.ShippingMethodId.Value, cancellationToken);

                if (shippingMethod == null || !shippingMethod.IsActive)
                    return ServiceResult.Failure("روش ارسال نامعتبر است.", 400);

                var shippingCost = shippingMethod.CalculateCost(order.TotalAmount);
                order.UpdateShippingMethod(shippingMethod.Id, shippingCost);
            }

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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