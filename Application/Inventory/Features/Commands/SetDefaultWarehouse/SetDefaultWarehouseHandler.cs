using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;

namespace Application.Inventory.Features.Commands.SetDefaultWarehouse;

public class SetDefaultWarehouseHandler(
    IWarehouseRepository warehouseRepository)
    : ICommandHandler<SetDefaultWarehouseCommand>
{
    public async Task<ServiceResult> Handle(SetDefaultWarehouseCommand request, CancellationToken ct)
    {
        var id = WarehouseId.From(request.Id);
        var warehouse = await warehouseRepository.GetByIdAsync(id, ct);

        if (warehouse is null)
            return ServiceResult.NotFound("انبار یافت نشد.");

        var current = await warehouseRepository.GetDefaultAsync(ct);
        if (current is not null && current.Id != id)
        {
            current.ClearDefault();
            warehouseRepository.Update(current);
        }

        warehouse.SetAsDefault();
        warehouseRepository.Update(warehouse);

        return ServiceResult.Success();
    }
}