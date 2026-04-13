using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetLowStockProducts;

public record GetLowStockProductsQuery(int Threshold = 5)
    : IRequest<ServiceResult<IEnumerable<LowStockItemDto>>>;