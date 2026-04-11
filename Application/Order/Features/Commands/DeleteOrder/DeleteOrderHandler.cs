using Domain.Common.Exceptions;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.DeleteOrder;

public class DeleteOrderHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<DeleteOrderCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        DeleteOrderCommand request,
        CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        try
        {
            order.Delete(request.UserId);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        await orderRepository.UpdateAsync(order, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogOrderEventAsync(
            order.Id,
            "DeleteOrder",
            request.UserId,
            $"سفارش {order.Id} حذف شد.");

        return ServiceResult.Success();
    }
}