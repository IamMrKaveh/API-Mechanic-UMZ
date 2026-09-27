using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Inventory.Features.Commands.ReconcileStock;

public class ReconcileStockHandler(
    IInventoryRepository inventoryRepository,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ReconcileStockCommand>
{
    public async Task<ServiceResult> Handle(ReconcileStockCommand request, CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(currentUserService.UserId!.Value);
        var stock = StockQuantity.Create(request.CalculatedStock);

        var inventoryResult = await (inventoryRepository.GetByVariantIdAsync(variantId, ct)).OrNotFoundAsync("موجودی یافت نشد.");
        if (inventoryResult.IsFailure) return inventoryResult.Error;
        var inventory = inventoryResult.Value;

        var result = InventoryDomainService.Reconcile(inventory, stock, userId, dateTimeProvider.UtcNow);

        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);

        await auditService.LogInventoryEventAsync(
            variantId,
            "ReconcileStock",
            $"انبارگردانی: موجودی محاسبه‌شده {request.CalculatedStock} واحد",
            userId);

        return ServiceResult.Success();
    }
}
