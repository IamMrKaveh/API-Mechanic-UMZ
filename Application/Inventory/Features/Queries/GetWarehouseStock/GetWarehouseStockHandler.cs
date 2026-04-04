using Application.Common.Results;
using Application.Inventory.Contracts;
using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetWarehouseStock;

public class GetWarehouseStockHandler(IInventoryQueryService inventoryQueryService)
        : IRequestHandler<GetWarehouseStockQuery, ServiceResult<IEnumerable<WarehouseStockDto>>>
{
    private readonly IInventoryQueryService _inventoryQueryService = inventoryQueryService;

    public async Task<ServiceResult<IEnumerable<WarehouseStockDto>>> Handle(
        GetWarehouseStockQuery request,
        CancellationToken cancellationToken)
    {
        var stocks = await _inventoryQueryService.GetWarehouseStockByVariantAsync(
            request.VariantId, cancellationToken);

        return ServiceResult<IEnumerable<WarehouseStockDto>>.Success(stocks);
    }
}