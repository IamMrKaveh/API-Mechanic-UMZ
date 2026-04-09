using Domain.Common.Exceptions;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.CancelOrder;

public class CancelOrderHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    ILogger<CancelOrderHandler> logger) : IRequestHandler<CancelOrderCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await orderRepository.FindByIdAsync(request.OrderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (!request.IsAdmin && order.UserId != request.UserId)
            return ServiceResult.Forbidden("دسترسی ممنوع.");

        if (!order.CanBeCancelled())
            return ServiceResult.Failure("این سفارش قابل لغو نیست.");

        try
        {
            order.Cancel(request.Reason);
            orderRepository.Update(order);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation("Order {OrderId} cancelled by user {UserId}", request.OrderId, request.UserId);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}