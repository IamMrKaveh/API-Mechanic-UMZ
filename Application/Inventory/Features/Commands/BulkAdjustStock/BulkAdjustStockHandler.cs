using Application.Audit.Contracts;

namespace Application.Inventory.Features.Commands.BulkAdjustStock;

public class BulkAdjustStockHandler : IRequestHandler<BulkAdjustStockCommand, ServiceResult<BulkAdjustResultDto>>
{
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;

    public BulkAdjustStockHandler(
        IInventoryService inventoryService,
        IAuditService auditService)
    {
        _inventoryService = inventoryService;
        _auditService = auditService;
    }

    public async Task<ServiceResult<BulkAdjustResultDto>> Handle(
        BulkAdjustStockCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.BulkAdjustStockAsync(
            request.Items,
            request.UserId,
            cancellationToken);

        if (result.IsSucceed)
        {
            await _auditService.LogInventoryEventAsync(
                0,
                "BulkStockAdjustment",
                $"تنظیم دسته‌ای موجودی: {result.Data!.SuccessCount} موفق، {result.Data.FailedCount} ناموفق از {result.Data.TotalRequested}",
                request.UserId);
        }

        return result;
    }
}