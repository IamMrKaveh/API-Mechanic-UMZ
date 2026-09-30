using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Inventory.Features.Commands.ToggleWarehouseActive;

public class ToggleWarehouseActiveHandler(
    IWarehouseRepository warehouseRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ToggleWarehouseActiveCommand>
{
    public async Task<ServiceResult> Handle(ToggleWarehouseActiveCommand request, CancellationToken ct)
    {
        var id = WarehouseId.From(request.Id);
        var warehouseResult = await (warehouseRepository.GetByIdAsync(id, ct)).OrNotFoundAsync("انبار یافت نشد.");
        if (warehouseResult.IsFailure) return warehouseResult.Error;
        var warehouse = warehouseResult.Value;

        var now = dateTimeProvider.UtcNow;

        if (request.IsActive)
            warehouse.Activate(now);
        else
            warehouse.Deactivate(now);

        warehouseRepository.Update(warehouse);

        return ServiceResult.Success();
    }
}