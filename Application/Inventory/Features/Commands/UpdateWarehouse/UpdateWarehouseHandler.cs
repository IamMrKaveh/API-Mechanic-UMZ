using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;

namespace Application.Inventory.Features.Commands.UpdateWarehouse;

public class UpdateWarehouseHandler(
    IWarehouseRepository warehouseRepository)
    : ICommandHandler<UpdateWarehouseCommand>
{
    public async Task<ServiceResult> Handle(UpdateWarehouseCommand request, CancellationToken ct)
    {
        var id = WarehouseId.From(request.Id);
        var warehouse = await warehouseRepository.GetByIdAsync(id, ct);

        if (warehouse is null)
            return ServiceResult.NotFound("انبار یافت نشد.");

        warehouse.Update(request.Name, request.City, request.Address, request.Phone, request.Priority);
        warehouseRepository.Update(warehouse);

        return ServiceResult.Success();
    }
}