namespace Application.Order.Features.Commands.DeleteOrderItem;

public class DeleteOrderItemHandler
    : IRequestHandler<DeleteOrderItemCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteOrderItemHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(DeleteOrderItemCommand request, CancellationToken ct)
    {
        var order = await _orderRepository.GetByOrderItemIdAsync(request.Id, ct);
        if (order == null)
            return ServiceResult.Failure("سفارش یا آیتم یافت نشد.", 404);

        try
        {
            order.RemoveItem(request.Id);
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