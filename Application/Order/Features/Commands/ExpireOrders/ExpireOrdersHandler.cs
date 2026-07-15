using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.ExpireOrders;

public class ExpireOrdersHandler(
    IOrderRepository orderRepository)
    : ICommandHandler<ExpireOrdersCommand, int>
{
    public async Task<ServiceResult<int>> Handle(ExpireOrdersCommand request, CancellationToken ct)
    {
        var pendingOrders = await orderRepository.FindPendingExpiredAsync(ct);
        var expiredCount = 0;

        foreach (var order in pendingOrders)
        {
            try
            {
                order.Expire(Domain.Order.ValueObjects.OrderStatusValue.Expired);
                orderRepository.Update(order);
                expiredCount++;
            }
            catch (DomainException)
            {
            }
        }

        return ServiceResult<int>.Success(expiredCount);
    }
}