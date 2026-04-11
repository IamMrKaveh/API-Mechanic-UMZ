using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Commands.AddStock;

public class AddStockHandler(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<AddStockCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(AddStockCommand request, CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(request.UserId);
        var stockQuantity = StockQuantity.Create(request.Quantity);

        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
            return ServiceResult.NotFound("موجودی یافت نشد.");

        var result = InventoryDomainService.IncreaseStock(
            inventory,
            stockQuantity,
            request.Notes,
            userId);

        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogInventoryEventAsync(
            variantId,
            "AddStock",
            $"Added {stockQuantity} units.",
            userId);

        return ServiceResult.Success();
    }
}