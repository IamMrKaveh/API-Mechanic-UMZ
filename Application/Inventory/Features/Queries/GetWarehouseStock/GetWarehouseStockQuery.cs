using Application.Common.Results;
using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetWarehouseStock;

public record GetWarehouseStockQuery(int VariantId)
    : IRequest<ServiceResult<IEnumerable<WarehouseStockDto>>>;