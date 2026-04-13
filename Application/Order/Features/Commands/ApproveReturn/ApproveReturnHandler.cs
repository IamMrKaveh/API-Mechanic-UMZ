using Domain.Common.Exceptions;
using Domain.Common.ValueObjects;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.ApproveReturn;

public class ApproveReturnHandler(
    IOrderRepository orderRepository,
    IInventoryService inventoryService,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<ApproveReturnCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        ApproveReturnCommand request,
        CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var order = await orderRepository.FindWithItemsByIdAsync(orderId, ct);
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

        return await unitOfWork.ExecuteStrategyAsync(async () =>
        {
            try
            {
                orderRepository.Update(order);
                await unitOfWork.SaveChangesAsync(ct);

                var result = await inventoryService.ReturnStockForOrderAsync(
                    orderId,
                    request.AdminUserId,
                    request.Reason,
                    ct);

                if (!result.IsSuccess)
                    return ServiceResult.Failure($"خطا در بازگشت موجودی: {result.Error}");

                await auditService.LogOrderEventAsync(
                    order.Id,
                    "ApproveReturn",
                    IpAddress.Create(request.IpAddress ?? "0.0.0.0"),
                    UserId.From(request.AdminUserId),
                    $"مرجوعی سفارش تأیید شد. دلیل: {request.Reason}");

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure(ex.Message);
            }
        }, ct);
    }
}