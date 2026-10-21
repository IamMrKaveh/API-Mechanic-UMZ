using Application.Common.Authorization;
using Application.Common.Interfaces;
using Domain.Order.Exceptions;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Commands.DeleteOrderItem;

public class DeleteOrderItemHandler(
    IOrderRepository orderRepository,
    ICurrentUserService currentUserService)
    : ICommandHandler<DeleteOrderItemCommand>
{
    public async Task<ServiceResult> Handle(
        DeleteOrderItemCommand request,
        CancellationToken ct)
    {
        var orderItemId = OrderItemId.From(request.Id);
        var orderResult = await (orderRepository.FindByOrderItemIdAsync(orderItemId, ct)).OrNotFoundAsync("سفارش یا آیتم یافت نشد.");
        if (orderResult.IsFailure) return orderResult.Error;
        var order = orderResult.Value;

        var ownership = AuthorizationGuard.EnsureOwnerOrAdmin(currentUserService, order.UserId);
        if (ownership.IsFailure) return ownership.Error;

        try
        {
            order.RemoveItem(orderItemId);

            orderRepository.Update(order);
            return ServiceResult.Success();
        }
        catch (OrderItemNotFoundException ex)
        {
            return ServiceResult.NotFound(ex.Message);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message, ErrorType.BusinessRule);
        }
    }
}
