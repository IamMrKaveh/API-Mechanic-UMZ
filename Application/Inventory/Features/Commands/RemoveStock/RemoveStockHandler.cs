using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Commands.RemoveStock;

public class RemoveStockHandler(
    IVariantRepository variantRepository,
    IInventoryRepository inventoryRepository,
    IAuditService auditService,
    IUnitOfWork unitOfWork,
    ICacheService cacheService) : IRequestHandler<RemoveStockCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(RemoveStockCommand request, CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(request.UserId);
        var stock = StockQuantity.Create(request.Quantity);

        var variant = await variantRepository.GetByIdAsync(variantId, ct);
        if (variant is null)
            return ServiceResult.NotFound("واریانت یافت نشد.");

        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
            return ServiceResult.NotFound("موجودی یافت نشد.");

        var result = InventoryDomainService.DecreaseStock(inventory, stock, request.Notes, userId);

        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);
        await unitOfWork.SaveChangesAsync(ct);

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