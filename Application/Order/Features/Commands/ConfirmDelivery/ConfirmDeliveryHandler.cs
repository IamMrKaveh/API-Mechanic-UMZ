using Application.Common.Results;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.ConfirmDelivery;

public class ConfirmDeliveryHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ConfirmDeliveryCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ConfirmDeliveryCommand request, CancellationToken ct)
    {
        var order = await orderRepository.FindByIdAsync(request.OrderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (order.UserId != request.UserId)
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