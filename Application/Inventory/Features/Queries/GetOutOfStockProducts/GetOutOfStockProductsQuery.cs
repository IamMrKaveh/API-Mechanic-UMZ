using Application.Common.Models;

namespace Application.Inventory.Features.Queries.GetOutOfStockProducts;

public record GetOutOfStockProductsQuery() : IRequest<ServiceResult<IEnumerable<OutOfStockItemDto>>>;