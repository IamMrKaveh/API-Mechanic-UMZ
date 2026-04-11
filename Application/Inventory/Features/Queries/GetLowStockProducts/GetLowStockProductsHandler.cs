namespace Application.Inventory.Features.Queries.GetLowStockProducts;

public class GetLowStockProductsHandler(IInventoryQueryService queryService)
        : IRequestHandler<GetLowStockProductsQuery, ServiceResult<IEnumerable<LowStockItemDto>>>
{
    public async Task<ServiceResult<IEnumerable<LowStockItemDto>>> Handle(
        GetLowStockProductsQuery request,
        CancellationToken ct)
    {
        var items = await queryService.GetLowStockProductsAsync(request.Threshold, ct);
        return ServiceResult<IEnumerable<LowStockItemDto>>.Success(items);
    }
}