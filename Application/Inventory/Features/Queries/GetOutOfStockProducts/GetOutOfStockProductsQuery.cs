using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetOutOfStockProducts;

public record GetOutOfStockProductsQuery() : IQuery<IEnumerable<OutOfStockItemDto>>;