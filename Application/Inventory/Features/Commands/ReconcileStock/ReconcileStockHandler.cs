namespace Application.Inventory.Features.Commands.ReconcileStock;

public class ReconcileStockHandler : IRequestHandler<ReconcileStockCommand, ServiceResult<ReconcileResultDto>>
{
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;

    public ReconcileStockHandler(
        IInventoryService inventoryService,
        IAuditService auditService)
    {
        _inventoryService = inventoryService;
        _auditService = auditService;
    }

    public async Task<ServiceResult<ReconcileResultDto>> Handle(
        ReconcileStockCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.ReconcileStockAsync(
            request.VariantId,
            request.UserId,
            cancellationToken);

        if (result.IsSucceed && result.Data != default)
        {
            var data = result.Data;

            if (data.HasDiscrepancy)
            {
                await _auditService.LogInventoryEventAsync(
                    request.VariantId,
                    "StockReconciled",
                    $"انبارگردانی: اختلاف {data.Difference} واحد اصلاح شد. موجودی نهایی: {data.FinalStock}",
                    request.UserId);
            }

            var dto = new ReconcileResultDto
            {
                VariantId = data.VariantId,
                FinalStock = data.FinalStock,
                Difference = data.Difference,
                HasDiscrepancy = data.HasDiscrepancy,
                Message = data.Message
            };

            return ServiceResult<ReconcileResultDto>.Success(dto);
        }

        return ServiceResult<ReconcileResultDto>.Failure(result.Error ?? "Failed", result.StatusCode);
    }
}