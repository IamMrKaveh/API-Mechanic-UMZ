using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

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
        var orderItemId = OrderItemId.From(request.Id);
        var order = await orderRepository.FindByOrderItemIdAsync(orderItemId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یا آیتم یافت نشد.");

        try
        {
            order.Items
                .FirstOrDefault(i => i.Id == orderItemId);

            orderRepository.Update(order);
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}