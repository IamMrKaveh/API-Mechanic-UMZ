using Application.Common.Results;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.ExpireOrders;

public class ExpireOrdersHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    ILogger<ExpireOrdersHandler> logger) : IRequestHandler<ExpireOrdersCommand, ServiceResult<int>>
{
    public async Task<ServiceResult<int>> Handle(ExpireOrdersCommand request, CancellationToken ct)
    {
        var pendingOrders = await orderRepository.FindPendingExpiredAsync(ct);
        var expiredCount = 0;

        foreach (var order in pendingOrders)
        {
            try
            {
                order.Expire();
                orderRepository.Update(order);
                expiredCount++;
            }
            catch (DomainException ex)
            {
                logger.LogWarning(ex, "Could not expire order {OrderId}", order.Id.Value);
            }
        }

        if (expiredCount > 0)
            await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("{Count} orders expired", expiredCount);
        return ServiceResult<int>.Success(expiredCount);
    }
}