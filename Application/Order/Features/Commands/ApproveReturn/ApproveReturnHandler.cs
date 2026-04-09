using Domain.Common.Exceptions;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.ApproveReturn;

public class ApproveReturnHandler(
    IOrderRepository orderRepository,
    IInventoryService inventoryService,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ILogger<ApproveReturnHandler> logger) : IRequestHandler<ApproveReturnCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IInventoryService _inventoryService = inventoryService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ILogger<ApproveReturnHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        ApproveReturnCommand request,
        CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, ct);
        if (order == null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        try
        {
            order.ValidateCanApproveReturn();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Forbidden(ex.Message);
        }

        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            try
            {
                order.ApproveReturn();
                await _orderRepository.UpdateAsync(order, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                var result = await _inventoryService.ReturnStockForOrderAsync(
                    request.OrderId,
                    request.AdminUserId,
                    request.Reason,
                    ct);

                if (result.IsFailure)
                {
                    _logger.LogError(
                        "Failed to return stock for Order {OrderId}: {Error}",
                        request.OrderId, result.Error);
                    return ServiceResult.Unexpected($"خطا در بازگشت موجودی: {result.Error}");
                }

                await _auditService.LogOrderEventAsync(
                    order.Id,
                    "ApproveReturn",
                    request.AdminUserId,
                    $"مرجوعی سفارش تأیید و موجودی به انبار بازگشت داده شد. دلیل: {request.Reason}");

                _logger.LogInformation(
                    "Return approved for Order {OrderId} by Admin {AdminId}. Stock returned.",
                    request.OrderId, request.AdminUserId);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving return for order {OrderId}", request.OrderId);
                return ServiceResult.Unexpected("خطایی در تأیید مرجوعی رخ داد.");
            }
        }, ct);
    }
}