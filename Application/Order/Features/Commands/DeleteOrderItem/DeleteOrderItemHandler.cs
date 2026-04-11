using Domain.Common.Exceptions;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.DeleteOrderItem;

public class DeleteOrderItemHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork)
        : IRequestHandler<DeleteOrderItemCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        DeleteOrderItemCommand request,
        CancellationToken ct)
    {
        var order = await orderRepository.GetByOrderItemIdAsync(request.Id, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یا آیتم یافت نشد.");

        try
        {
            order.RemoveItem(request.Id);
            await orderRepository.UpdateAsync(order, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}