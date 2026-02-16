namespace Application.Order.Features.Commands.DeleteOrderItem;

public class DeleteOrderItemHandler : IRequestHandler<DeleteOrderItemCommand, ServiceResult>
{
    private readonly LedkaContext _context;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteOrderItemHandler(LedkaContext context, IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _context = context;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(DeleteOrderItemCommand request, CancellationToken ct)
    {
        var orderItem = await _context.OrderItems.FirstOrDefaultAsync(oi => oi.Id == request.Id, ct);
        if (orderItem == null) return ServiceResult.Failure("آیتم یافت نشد.", 404);

        var order = await _orderRepository.GetByIdWithItemsAsync(orderItem.OrderId, ct);
        if (order == null) return ServiceResult.Failure("سفارش یافت نشد.", 404);

        try
        {
            order.RemoveItem(request.Id);
            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message, 400);
        }
    }
}