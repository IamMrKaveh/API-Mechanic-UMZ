using Application.Common.Models;

namespace Application.Inventory.Features.Queries.GetWarehouseStock;

public record GetWarehouseStockQuery(int VariantId)
    : IRequest<ServiceResult<IEnumerable<WarehouseStockDto>>>;