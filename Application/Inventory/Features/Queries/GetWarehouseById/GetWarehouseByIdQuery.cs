using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetWarehouseById;

public record GetWarehouseByIdQuery(Guid Id) : IQuery<WarehouseDto>;