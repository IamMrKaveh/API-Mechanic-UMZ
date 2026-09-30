using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Inventory.Features.Commands.SetDefaultWarehouse;

public class SetDefaultWarehouseHandler(
    IWarehouseRepository warehouseRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<SetDefaultWarehouseCommand>
{
    public async Task<ServiceResult> Handle(SetDefaultWarehouseCommand request, CancellationToken ct)
    {
        var id = WarehouseId.From(request.Id);
        var warehouseResult = await (warehouseRepository.GetByIdAsync(id, ct)).OrNotFoundAsync("انبار یافت نشد.");
        if (warehouseResult.IsFailure) return warehouseResult.Error;
        var warehouse = warehouseResult.Value;

        var now = dateTimeProvider.UtcNow;

        var current = await warehouseRepository.GetDefaultAsync(ct);
        if (current is not null && current.Id != id)
        {
            current.ClearDefault(now);
            warehouseRepository.Update(current);
        }

        warehouse.SetAsDefault(now);
        warehouseRepository.Update(warehouse);

        return ServiceResult.Success();
    }
}