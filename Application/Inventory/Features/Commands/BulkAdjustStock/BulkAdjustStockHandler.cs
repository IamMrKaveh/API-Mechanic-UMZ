namespace Application.Inventory.Features.Commands.BulkAdjustStock;

public class BulkAdjustStockHandler : IRequestHandler<BulkAdjustStockCommand, ServiceResult<BulkAdjustResultDto>>
{
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;

    public BulkAdjustStockHandler(
        IInventoryService inventoryService,
        IAuditService auditService
        )
    {
        _inventoryService = inventoryService;
        _auditService = auditService;
    }

    public async Task<ServiceResult<BulkAdjustResultDto>> Handle(
        BulkAdjustStockCommand request,
        CancellationToken ct
        )
    {
        var mappedItems = request.Items.Select(x => (x.VariantId, x.QuantityChange, x.Notes));

        var result = await _inventoryService.BulkAdjustStockAsync(
            mappedItems,
            request.UserId,
            ct);

        if (result.IsSucceed && result.Data != default)
        {
            var data = result.Data;

            await _auditService.LogInventoryEventAsync(
                0,
                "BulkStockAdjustment",
                $"تنظیم دسته‌ای موجودی: {data.Success} موفق، {data.Failed} ناموفق از {data.Total}",
                request.UserId);

            var dto = new BulkAdjustResultDto
            {
                TotalRequested = data.Total,
                SuccessCount = data.Success,
                FailedCount = data.Failed,
                Results = data.Results.Select(r => new BulkAdjustItemResultDto
                {
                    VariantId = r.VariantId,
                    IsSuccess = r.IsSuccess,
                    Error = r.Error,
                    NewStock = r.NewStock ?? 0
                })
                .ToList()
            };

            return ServiceResult<BulkAdjustResultDto>.Success(dto);
        }

        return ServiceResult<BulkAdjustResultDto>.Failure(result.Error ?? "Failed", result.StatusCode);
    }
}