using Application.Audit.Contracts;
using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Commands.ReconcileStock;

public class ReconcileStockHandler(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<ReconcileStockCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ReconcileStockCommand request, CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(request.UserId);
        var stock = StockQuantity.Create(request.CalculatedStock);

        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
            return ServiceResult.NotFound("موجودی یافت نشد.");

        var result = InventoryDomainService.Reconcile(inventory, stock, userId);

        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogInventoryEventAsync(
            variantId,
            "ReconcileStock",
            $"انبارگردانی: موجودی محاسبه‌شده {request.CalculatedStock} واحد",
            userId);

        return ServiceResult.Success();
    }
}