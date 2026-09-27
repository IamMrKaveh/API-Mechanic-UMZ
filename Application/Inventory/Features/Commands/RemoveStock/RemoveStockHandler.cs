using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Inventory.Features.Commands.RemoveStock;

public class RemoveStockHandler(
    IVariantRepository variantRepository,
    IInventoryRepository inventoryRepository,
    IAuditService auditService,
    ICacheService cacheService,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RemoveStockCommand>
{
    public async Task<ServiceResult> Handle(RemoveStockCommand request, CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(currentUserService.UserId!.Value);
        var stock = StockQuantity.Create(request.Quantity);

        var variantResult = await (variantRepository.GetByIdAsync(variantId, ct)).OrNotFoundAsync("واریانت یافت نشد.");
        if (variantResult.IsFailure) return variantResult.Error;
        var variant = variantResult.Value;

        var inventoryResult = await (inventoryRepository.GetByVariantIdAsync(variantId, ct)).OrNotFoundAsync("موجودی یافت نشد.");
        if (inventoryResult.IsFailure) return inventoryResult.Error;
        var inventory = inventoryResult.Value;

        var result = InventoryDomainService.DecreaseStock(inventory, stock, request.Notes, dateTimeProvider.UtcNow, userId);

        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);

        await auditService.LogInventoryEventAsync(
            variantId,
            "RemoveStock",
            $"Removed {stock} units from variant {request.VariantId}.",
            userId);

        await cacheService.RemoveAsync($"product:{variant.ProductId.Value}", ct);
        await cacheService.RemoveAsync($"variant:{request.VariantId}", ct);

        return ServiceResult.Success();
    }
}
