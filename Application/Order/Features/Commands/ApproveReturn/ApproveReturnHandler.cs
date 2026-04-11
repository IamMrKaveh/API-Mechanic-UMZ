using Domain.Common.Exceptions;
using Domain.Common.ValueObjects;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.ApproveReturn;

public class ApproveReturnHandler(
    IOrderRepository orderRepository,
    IInventoryService inventoryService,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ILogger<ApproveReturnHandler> logger) : IRequestHandler<ApproveReturnCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        ApproveReturnCommand request,
        CancellationToken ct)
    {
        var order = await orderRepository.GetByIdWithItemsAsync(request.OrderId, ct);
        if (order is null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        try
        {
            order.ValidateCanApproveReturn();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Forbidden(ex.Message);
        }

        return await unitOfWork.ExecuteStrategyAsync(async () =>
        {
            try
            {
                order.ApproveReturn();
                await orderRepository.UpdateAsync(order, ct);
                await unitOfWork.SaveChangesAsync(ct);

                var result = await inventoryService.ReturnStockForOrderAsync(
                    request.OrderId,
                    request.AdminUserId,
                    request.Reason,
                    ct);

                if (result.IsFailure)
                {
                    logger.LogError(
                        "Failed to return stock for Order {OrderId}: {Error}",
                        request.OrderId, result.Error);
                    return ServiceResult.Failure($"خطا در بازگشت موجودی: {result.Error}");
                }

                await auditService.LogOrderEventAsync(
                    order.Id,
                    "ApproveReturn",
                    IpAddress.Create(request.IpAddress),
                    request.AdminUserId,
                    $"مرجوعی سفارش تأیید و موجودی به انبار بازگشت داده شد. دلیل: {request.Reason}");

                logger.LogInformation(
                    "Return approved for Order {OrderId} by Admin {AdminId}. Stock returned.",
                    request.OrderId, request.AdminUserId);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error approving return for order {OrderId}", request.OrderId);
                return ServiceResult.Failure("خطایی در تأیید مرجوعی رخ داد.");
            }
        }, ct);
    }
}