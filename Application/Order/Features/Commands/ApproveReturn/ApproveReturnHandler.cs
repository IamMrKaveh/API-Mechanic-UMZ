namespace Application.Order.Features.Commands.ApproveReturn;

public class ApproveReturnHandler : IRequestHandler<ApproveReturnCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<ApproveReturnHandler> _logger;

    public ApproveReturnHandler(
        IOrderRepository orderRepository,
        IInventoryService inventoryService,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<ApproveReturnHandler> logger)
    {
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        ApproveReturnCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, cancellationToken);
        if (order == null)
            return ServiceResult.Failure("سفارش یافت نشد.", 404);

        // اطمینان از اینکه سفارش در وضعیت Returned است
        if (order.Status.Value != "Returned")
            return ServiceResult.Failure("سفارش باید ابتدا توسط کاربر درخواست مرجوعی داده شده باشد.", 400);

        // بازگشت موجودی برای تمام آیتم‌ها
        var result = await _inventoryService.ReturnStockForOrderAsync(
            request.OrderId,
            request.AdminUserId,
            request.Reason,
            cancellationToken);

        if (result.IsFailed)
        {
            _logger.LogError(
                "Failed to return stock for Order {OrderId}: {Error}",
                request.OrderId, result.Error);
            return ServiceResult.Failure($"خطا در بازگشت موجودی: {result.Error}");
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
}