using Application.Audit.Contracts;

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

        if (result.IsSucceed && result.Data!.HasDiscrepancy)
        {
            await _auditService.LogInventoryEventAsync(
                request.VariantId,
                "StockReconciled",
                $"انبارگردانی: اختلاف {result.Data.Difference} واحد اصلاح شد. موجودی نهایی: {result.Data.FinalStock}",
                request.UserId);
        }

        return result;
    }
}