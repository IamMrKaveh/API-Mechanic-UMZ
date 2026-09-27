using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.DeleteOrderItem;

public class DeleteOrderItemHandler(
    IOrderRepository orderRepository)
    : ICommandHandler<DeleteOrderItemCommand>
{
    public async Task<ServiceResult> Handle(
        DeleteOrderItemCommand request,
        CancellationToken ct)
    {
        var orderItemId = OrderItemId.From(request.Id);
        var orderResult = await (orderRepository.FindByOrderItemIdAsync(orderItemId, ct)).OrNotFoundAsync("سفارش یا آیتم یافت نشد.");
        if (orderResult.IsFailure) return orderResult.Error;
        var order = orderResult.Value;

        try
        {
            order.OrderItems
                .FirstOrDefault(i => i.Id == orderItemId);

            orderRepository.Update(order);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}