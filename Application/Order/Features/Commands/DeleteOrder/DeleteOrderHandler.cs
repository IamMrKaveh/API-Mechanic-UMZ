using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.DeleteOrder;

public class DeleteOrderHandler(
    IOrderRepository orderRepository,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<DeleteOrderCommand>
{
    public async Task<ServiceResult> Handle(
        DeleteOrderCommand request,
        CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var orderResult = await (orderRepository.FindByIdAsync(orderId, ct)).OrNotFoundAsync("سفارش یافت نشد.");
        if (orderResult.IsFailure) return orderResult.Error;
        var order = orderResult.Value;

        try
        {
            order.MarkAsDeleted();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        orderRepository.Update(order);

        await auditService.LogOrderEventAsync(
            order.Id,
            "DeleteOrder",
            IpAddress.Unknown,
            UserId.From(currentUserService.UserId!.Value),
            $"سفارش {order.Id.Value} حذف شد.",
            ct);

        return ServiceResult.Success();
    }
}
