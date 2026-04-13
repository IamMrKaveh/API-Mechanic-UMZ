using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

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
        var orderId = OrderId.From(request.OrderId);
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        try
        {
            order.MarkAsDeleted();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogOrderEventAsync(
            order.Id,
            "DeleteOrder",
            IpAddress.Unknown,
            UserId.From(request.UserId),
            $"سفارش {order.Id.Value} حذف شد.");

        return ServiceResult.Success();
    }
}