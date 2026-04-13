using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.ConfirmDelivery;

public class ConfirmDeliveryHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ConfirmDeliveryCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ConfirmDeliveryCommand request, CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var userId = UserId.From(request.UserId);

        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (order.UserId != userId)
            return ServiceResult.Forbidden("دسترسی ممنوع.");

        try
        {
            order.MarkAsDelivered();
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