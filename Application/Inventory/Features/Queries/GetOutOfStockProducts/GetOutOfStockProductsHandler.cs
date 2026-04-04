using Application.Common.Results;

namespace Application.Inventory.Features.Queries.GetOutOfStockProducts;

public class GetOutOfStockProductsHandler(IInventoryQueryService queryService) : IRequestHandler<GetOutOfStockProductsQuery, ServiceResult<IEnumerable<OutOfStockItemDto>>>
{
    private readonly IInventoryQueryService _queryService = queryService;

    public async Task<ServiceResult<IEnumerable<OutOfStockItemDto>>> Handle(
        GetOutOfStockProductsQuery request,
        CancellationToken ct)
    {
        var result = await _queryService.GetOutOfStockProductsAsync(ct);
        return ServiceResult<IEnumerable<OutOfStockItemDto>>.Success(result);
    }
}