using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetAllWarehouses;

public record GetAllWarehousesQuery : IRequest<ServiceResult<IReadOnlyList<WarehouseDto>>>;