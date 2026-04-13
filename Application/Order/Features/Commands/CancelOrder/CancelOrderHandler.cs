using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.CancelOrder;

public class CancelOrderHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelOrderCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (!request.IsAdmin && order.UserId != UserId.From(request.UserId))
            return ServiceResult.Forbidden("دسترسی ممنوع.");

        if (!order.CanBeCancelled())
            return ServiceResult.Failure("این سفارش قابل لغو نیست.");

        try
        {
            order.Cancel(request.Reason);
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