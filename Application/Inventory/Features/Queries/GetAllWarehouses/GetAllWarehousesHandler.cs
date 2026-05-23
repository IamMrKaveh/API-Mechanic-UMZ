using Application.Inventory.Features.Shared;
using Domain.Inventory.Interfaces;

namespace Application.Inventory.Features.Queries.GetAllWarehouses;

public class GetAllWarehousesHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<GetAllWarehousesQuery, ServiceResult<IReadOnlyList<WarehouseDto>>>
{
    public async Task<ServiceResult<IReadOnlyList<WarehouseDto>>> Handle(
        GetAllWarehousesQuery request,
        CancellationToken ct)
    {
        var warehouses = await warehouseRepository.GetAllAsync(ct);

        var dtos = warehouses.Select(w => new WarehouseDto(
            w.Id.Value,
            w.Code.Value,
            w.Name,
            w.City,
            w.Address,
            w.Phone,
            w.Priority,
            w.IsActive,
            w.IsDefault,
            w.CreatedAt
        )).ToList();

        return ServiceResult<IReadOnlyList<WarehouseDto>>.Success(dtos);
    }
}