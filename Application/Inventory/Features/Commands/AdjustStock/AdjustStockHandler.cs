namespace Application.Inventory.Features.Commands.AdjustStock;

public class AdjustStockHandler : IRequestHandler<AdjustStockCommand, ServiceResult>
{
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;

    public AdjustStockHandler(
        IInventoryService inventoryService,
        IAuditService auditService)
    {
        _inventoryService = inventoryService;
        _auditService = auditService;
    }

    public async Task<ServiceResult> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.AdjustStockAsync(
            request.VariantId,
            request.QuantityChange,
            request.UserId,
            request.Notes,
            cancellationToken);

        if (result.IsSucceed)
        {
            await _auditService.LogInventoryEventAsync(
                request.VariantId,
                "StockAdjustment",
                $"تنظیم دستی موجودی: {request.QuantityChange:+#;-#;0} واحد. توضیحات: {request.Notes}",
                request.UserId);
        }

        return result;
    }
}