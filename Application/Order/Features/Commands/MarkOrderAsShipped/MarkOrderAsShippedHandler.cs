using Application.Common.Results;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.MarkOrderAsShipped;

public class MarkOrderAsShippedHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<MarkOrderAsShippedCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(MarkOrderAsShippedCommand request, CancellationToken ct)
    {
        var order = await orderRepository.FindByIdAsync(request.OrderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        try
        {
            order.MarkAsShipped();
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