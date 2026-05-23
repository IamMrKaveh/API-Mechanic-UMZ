using Application.Inventory.Features.Shared;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;

namespace Application.Inventory.Features.Queries.GetWarehouseById;

public class GetWarehouseByIdHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<GetWarehouseByIdQuery, ServiceResult<WarehouseDto>>
{
    public async Task<ServiceResult<WarehouseDto>> Handle(
        GetWarehouseByIdQuery request,
        CancellationToken ct)
    {
        var id = WarehouseId.From(request.Id);
        var warehouse = await warehouseRepository.GetByIdAsync(id, ct);

        if (warehouse is null)
            return ServiceResult<WarehouseDto>.NotFound("انبار یافت نشد.");

        var dto = new WarehouseDto(
            warehouse.Id.Value,
            warehouse.Code.Value,
            warehouse.Name,
            warehouse.City,
            warehouse.Address,
            warehouse.Phone,
            warehouse.Priority,
            warehouse.IsActive,
            warehouse.IsDefault,
            warehouse.CreatedAt
        );

        return ServiceResult<WarehouseDto>.Success(dto);
    }
}