namespace Application.Inventory.Features.Queries.GetWarehouseStock;

public class GetWarehouseStockHandler(IInventoryQueryService inventoryQueryService)
        : IRequestHandler<GetWarehouseStockQuery, ServiceResult<IEnumerable<WarehouseStockDto>>>
{
    public async Task<ServiceResult<IEnumerable<WarehouseStockDto>>> Handle(
        GetWarehouseStockQuery request,
        CancellationToken cancellationToken)
    {
        var stocks = await inventoryQueryService.GetWarehouseStockByVariantAsync(
            request.VariantId, cancellationToken);

        return ServiceResult<IEnumerable<WarehouseStockDto>>.Success(stocks);
    }
}