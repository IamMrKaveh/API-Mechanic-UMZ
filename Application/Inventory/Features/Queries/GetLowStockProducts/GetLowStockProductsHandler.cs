using Application.Common.Models;

namespace Application.Inventory.Features.Queries.GetLowStockProducts;

public class GetLowStockProductsHandler(IInventoryQueryService queryService)
        : IRequestHandler<GetLowStockProductsQuery, ServiceResult<IEnumerable<LowStockItemDto>>>
{
    private readonly IInventoryQueryService _queryService = queryService;

    public async Task<ServiceResult<IEnumerable<LowStockItemDto>>> Handle(
        GetLowStockProductsQuery request,
        CancellationToken ct)
    {
        var items = await _queryService.GetLowStockProductsAsync(request.Threshold, ct);
        return ServiceResult<IEnumerable<LowStockItemDto>>.Success(items);
    }
}