using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetOutOfStockProducts;

public class GetOutOfStockProductsHandler(IInventoryQueryService queryService)
    : IRequestHandler<GetOutOfStockProductsQuery, ServiceResult<IEnumerable<OutOfStockItemDto>>>
{
    public async Task<ServiceResult<IEnumerable<OutOfStockItemDto>>> Handle(
        GetOutOfStockProductsQuery request,
        CancellationToken ct)
    {
        var result = await queryService.GetOutOfStockProductsAsync(ct);
        return ServiceResult<IEnumerable<OutOfStockItemDto>>.Success(result);
    }
}