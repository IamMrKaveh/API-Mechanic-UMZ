using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.ApproveReturn;

public class ApproveReturnHandler(
    IOrderRepository orderRepository,
    IInventoryService inventoryService,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<ApproveReturnCommand>
{
    public async Task<ServiceResult> Handle(
        ApproveReturnCommand request,
        CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        var userId = UserId.From(currentUserService.UserId.Value);

        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        try
        {
            order.MarkAsReturned();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Forbidden(ex.Message);
        }

        try
        {
            return await unitOfWork.ExecuteStrategyAsync(async cancellationToken =>
            {
                orderRepository.Update(order);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                var result = await inventoryService.ReturnStockForOrderAsync(
                    orderId,
                    userId.Value,
                    request.Reason,
                    cancellationToken);

                if (!result.IsSuccess)
                    return ServiceResult.Failure($"خطا در بازگشت موجودی: {result.Error}");

                await auditService.LogOrderEventAsync(
                    order.Id,
                    "ApproveReturn",
                    IpAddress.Create(currentUserService.IpAddress ?? "0.0.0.0"),
                    UserId.From(currentUserService.UserId.Value),
                    $"مرجوعی سفارش تأیید شد. دلیل: {request.Reason}");

                return ServiceResult.Success();
            }, ct);
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}