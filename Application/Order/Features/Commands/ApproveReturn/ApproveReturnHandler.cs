using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.ApproveReturn;

public class ApproveReturnHandler(
    IOrderRepository orderRepository,
    IInventoryService inventoryService,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : ICommandHandler<ApproveReturnCommand>
{
    public async Task<ServiceResult> Handle(
        ApproveReturnCommand request,
        CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var orderResult = await orderRepository.FindByIdAsync(orderId, ct).OrNotFoundAsync("سفارش یافت نشد.");
        var userId = UserId.From(currentUserService.UserId!.Value);

        if (orderResult.IsFailure) return orderResult.ToServiceResult();
        var order = orderResult.Value;

        try
        {
            order.MarkAsReturned();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Forbidden(ex.Message);
        }

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);

        var result = await inventoryService.ReturnStockForOrderAsync(
            orderId,
            userId.Value,
            request.Reason,
            ct);

        if (!result.IsSuccess)
            return ServiceResult.Failure($"خطا در بازگشت موجودی: {result.Error}");

        return ServiceResult.Success();
    }
}
