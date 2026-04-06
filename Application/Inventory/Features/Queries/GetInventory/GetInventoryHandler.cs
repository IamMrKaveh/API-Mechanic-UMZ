using Application.Common.Results;
using Application.Inventory.Contracts;
using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetInventory;

public class GetInventoryHandler(
    IInventoryQueryService inventoryQueryService) : IRequestHandler<GetInventoryQuery, ServiceResult<InventoryDto>>
{
    private readonly IInventoryQueryService _inventoryQueryService = inventoryQueryService;

    public async Task<ServiceResult<InventoryDto>> Handle(
        GetInventoryQuery request,
        CancellationToken ct)
    {
        var inventory = await _inventoryQueryService.GetVariantAvailabilityAsync(request.VariantId, ct);
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