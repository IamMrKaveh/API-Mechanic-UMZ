namespace Application.Order.Features.Commands.CancelOrder;

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IDiscountService _discountService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OrderDomainService _orderDomainService;
    private readonly ILogger<CancelOrderHandler> _logger;

    public CancelOrderHandler(
        IOrderRepository orderRepository,
        IInventoryService inventoryService,
        IDiscountService discountService,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        OrderDomainService orderDomainService,
        ILogger<CancelOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
        _discountService = discountService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _orderDomainService = orderDomainService;
        _logger = logger;
    }

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
            using var transaction = await _unitOfWork.BeginTransactionAsync();

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
                await _unitOfWork.CommitTransactionAsync(ct);

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
                await _unitOfWork.RollbackTransactionAsync(ct);
                _logger.LogError(ex, "Error cancelling order {OrderId}", request.OrderId);
                return ServiceResult.Failure("خطایی در لغو سفارش رخ داد.");
            }
        }, ct);
    }
}