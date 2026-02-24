namespace Application.Inventory.Features.Queries.GetOutOfStockProducts;

public class GetOutOfStockProductsHandler : IRequestHandler<GetOutOfStockProductsQuery, ServiceResult<IEnumerable<OutOfStockItemDto>>>
{
    private readonly IInventoryQueryService _queryService;

    public GetOutOfStockProductsHandler(IInventoryQueryService queryService) => _queryService = queryService;

    public async Task<ServiceResult<IEnumerable<OutOfStockItemDto>>> Handle(GetOutOfStockProductsQuery request, CancellationToken ct)
    {
        var result = await _queryService.GetOutOfStockProductsAsync(ct);
        return ServiceResult<IEnumerable<OutOfStockItemDto>>.Success(result);
    }
}