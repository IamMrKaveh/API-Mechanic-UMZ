using Application.Inventory.Features.Shared;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Queries.GetWarehouseStock;

public class GetWarehouseStockHandler(IInventoryQueryService inventoryQueryService)
    : IRequestHandler<GetWarehouseStockQuery, ServiceResult<IEnumerable<WarehouseStockDto>>>
{
    public async Task<ServiceResult<IEnumerable<WarehouseStockDto>>> Handle(
        GetWarehouseStockQuery request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var stocks = await inventoryQueryService.GetWarehouseStockByVariantAsync(
            variantId, ct);

        return ServiceResult<IEnumerable<WarehouseStockDto>>.Success(stocks);
    }
}