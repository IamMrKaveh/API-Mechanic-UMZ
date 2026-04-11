namespace Application.Inventory.Features.Queries.GetWarehouseStock;

public record GetWarehouseStockQuery(Guid VariantId) : IRequest<ServiceResult<IEnumerable<WarehouseStockDto>>>;