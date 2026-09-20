using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Inventory.Features.Commands.ToggleWarehouseActive;

public class ToggleWarehouseActiveHandler(
    IWarehouseRepository warehouseRepository,
    ICacheService cacheService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ToggleWarehouseActiveCommand>
{
    public async Task<ServiceResult> Handle(ToggleWarehouseActiveCommand request, CancellationToken ct)
    {
        var id = WarehouseId.From(request.Id);
        var warehouse = await warehouseRepository.GetByIdAsync(id, ct);

        if (warehouse is null)
            return ServiceResult.NotFound("انبار یافت نشد.");

        var now = dateTimeProvider.UtcNow;

        if (request.IsActive)
            warehouse.Activate(now);
        else
            warehouse.Deactivate(now);

        warehouseRepository.Update(warehouse);
        await cacheService.RemoveByPrefixAsync("warehouses:", ct);

        return ServiceResult.Success();
    }
}