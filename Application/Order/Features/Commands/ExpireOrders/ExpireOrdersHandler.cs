using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.ExpireOrders;

public class ExpireOrdersHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ExpireOrdersCommand, ServiceResult<int>>
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

        if (expiredCount > 0)
            await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<int>.Success(expiredCount);
    }
}