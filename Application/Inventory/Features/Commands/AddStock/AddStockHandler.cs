using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Inventory.Features.Commands.AddStock;

public class AddStockHandler(
    IInventoryRepository inventoryRepository,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AddStockCommand>
{
    public async Task<ServiceResult> Handle(AddStockCommand request, CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(currentUserService.UserId!.Value);
        var stockQuantity = StockQuantity.Create(request.Quantity);

        var inventoryResult = await (inventoryRepository.GetByVariantIdAsync(variantId, ct)).OrNotFoundAsync("موجودی یافت نشد.");
        if (inventoryResult.IsFailure) return inventoryResult.Error;
        var inventory = inventoryResult.Value;

        var result = InventoryDomainService.IncreaseStock(
            inventory,
            stockQuantity,
            request.Notes,
            dateTimeProvider.UtcNow,
            userId);

        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);

        await auditService.LogInventoryEventAsync(
            variantId,
            "AddStock",
            $"Added {stockQuantity} units.",
            userId);

        return ServiceResult.Success();
    }
}
