using Domain.Common.Exceptions;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.DeleteOrderItem;

public class DeleteOrderItemHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork)
        : IRequestHandler<DeleteOrderItemCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        DeleteOrderItemCommand request,
        CancellationToken ct)
    {
        var order = await _orderRepository.GetByOrderItemIdAsync(request.Id, ct);
        if (order == null)
            return ServiceResult.NotFound("سفارش یا آیتم یافت نشد.");

        try
        {
            order.RemoveItem(request.Id);
            await _orderRepository.UpdateAsync(order, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }
    }
}