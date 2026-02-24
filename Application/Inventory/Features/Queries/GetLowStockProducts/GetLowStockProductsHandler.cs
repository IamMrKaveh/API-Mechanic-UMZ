namespace Application.Inventory.Features.Queries.GetLowStockProducts;

public class GetLowStockProductsHandler
    : IRequestHandler<GetLowStockProductsQuery, ServiceResult<IEnumerable<LowStockItemDto>>>
{
    private readonly IInventoryQueryService _queryService;

    public GetLowStockProductsHandler(IInventoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<IEnumerable<LowStockItemDto>>> Handle(
        GetLowStockProductsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _queryService.GetLowStockProductsAsync(request.Threshold, cancellationToken);
        return ServiceResult<IEnumerable<LowStockItemDto>>.Success(items);
    }
}