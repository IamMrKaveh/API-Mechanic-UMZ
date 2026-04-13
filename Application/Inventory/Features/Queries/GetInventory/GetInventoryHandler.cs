using Application.Inventory.Features.Shared;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Queries.GetInventory;

public class GetInventoryHandler(IInventoryQueryService inventoryQueryService) : IRequestHandler<GetInventoryQuery, ServiceResult<InventoryDto>>
{
    public async Task<ServiceResult<InventoryDto>> Handle(
        GetInventoryQuery request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var inventory = await inventoryQueryService.GetVariantAvailabilityAsync(variantId, ct);
        return inventory is null
            ? ServiceResult<InventoryDto>.NotFound("موجودی یافت نشد.")
            : ServiceResult<InventoryDto>.Success(new InventoryDto
            {
                VariantId = inventory.VariantId,
                StockQuantity = inventory.OnHand,
                ReservedQuantity = inventory.Reserved,
                AvailableQuantity = inventory.Available,
                IsUnlimited = inventory.IsUnlimited
            });
    }
}