namespace Application.Order.Features.Commands.UpdateOrderItem;

public class UpdateOrderItemHandler
    : IRequestHandler<UpdateOrderItemCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrderItemHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(UpdateOrderItemCommand request, CancellationToken ct)
    {
        var order = await _orderRepository.GetByOrderItemIdAsync(request.Id, ct);
        if (order == null)
            return ServiceResult.Failure("سفارش یا آیتم یافت نشد.", 404);

        try
        {
            order.UpdateItemQuantity(request.Id, request.Dto.Quantity);
            await _orderRepository.UpdateAsync(order, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message, 400);
        }
    }
}