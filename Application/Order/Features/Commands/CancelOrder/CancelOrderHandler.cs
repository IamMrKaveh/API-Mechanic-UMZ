using Application.Common.Models;

namespace Application.Order.Features.Commands.CancelOrder;

public class CancelOrderHandler(
    IOrderRepository orderRepository,
    IInventoryService inventoryService,
    IDiscountService discountService,
    IAuditService auditService,
    IUnitOfWork unitOfWork,
    OrderDomainService orderDomainService,
    ILogger<CancelOrderHandler> logger) : IRequestHandler<CancelOrderCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IInventoryService _inventoryService = inventoryService;
    private readonly IDiscountService _discountService = discountService;
    private readonly IAuditService _auditService = auditService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly OrderDomainService _orderDomainService = orderDomainService;
    private readonly ILogger<CancelOrderHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        CancelOrderCommand request,
        CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, ct);

        if (order == null)
            return ServiceResult.Failure("سفارش یافت نشد.", 404);

        var validation = _orderDomainService.ValidateCancellation(order, request.UserId, request.IsAdmin);
        if (!validation.CanCancel)
            return ServiceResult.Failure(validation.Error!, 400);

        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            try
            {
                order.Cancel(request.UserId, request.Reason);

                await _inventoryService.RollbackReservationsAsync($"ORDER-{order.Id}");

                if (order.DiscountCodeId.HasValue)
                {
                    await _discountService.CancelDiscountUsageAsync(order.Id);
                }

                await _orderRepository.UpdateAsync(order, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                await _auditService.LogOrderEventAsync(
                    order.Id,
                    "CancelOrder",
                    request.UserId,
                    $"سفارش توسط {(request.IsAdmin ? "مدیر" : "کاربر")} لغو شد. دلیل: {request.Reason}");

                _logger.LogInformation(
                    "Order {OrderId} cancelled by user {UserId}. Reason: {Reason}",
                    order.Id, request.UserId, request.Reason);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", request.OrderId);
                return ServiceResult.Failure("خطایی در لغو سفارش رخ داد.");
            }
        }, ct);
    }
}