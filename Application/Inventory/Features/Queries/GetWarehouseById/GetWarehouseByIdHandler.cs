using Application.Inventory.Features.Shared;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;

namespace Application.Inventory.Features.Queries.GetWarehouseById;

public class GetWarehouseByIdHandler(IWarehouseRepository warehouseRepository)
    : IQueryHandler<GetWarehouseByIdQuery, WarehouseDto>
{
    public async Task<ServiceResult<WarehouseDto>> Handle(
        GetWarehouseByIdQuery request,
        CancellationToken ct)
    {
        var id = WarehouseId.From(request.Id);
        var warehouseResult = await (warehouseRepository.GetByIdAsync(id, ct)).OrNotFoundAsync("انبار یافت نشد.");
        if (warehouseResult.IsFailure) return warehouseResult.Error;
        var warehouse = warehouseResult.Value;

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