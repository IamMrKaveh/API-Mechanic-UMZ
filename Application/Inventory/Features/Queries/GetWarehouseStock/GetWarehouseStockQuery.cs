using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetWarehouseStock;

public record GetWarehouseStockQuery(Guid VariantId)
    : IQuery<IEnumerable<WarehouseStockDto>>;