using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Inventory.Features.Commands.UpdateWarehouse;

public class UpdateWarehouseHandler(
    IWarehouseRepository warehouseRepository,
    ICacheService cacheService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateWarehouseCommand>
{
    public async Task<ServiceResult> Handle(UpdateWarehouseCommand request, CancellationToken ct)
    {
        var id = WarehouseId.From(request.Id);
        var warehouseResult = await (warehouseRepository.GetByIdAsync(id, ct)).OrNotFoundAsync("انبار یافت نشد.");
        if (warehouseResult.IsFailure) return warehouseResult.Error;
        var warehouse = warehouseResult.Value;

        warehouse.Update(request.Name, request.City, request.Address, request.Phone, request.Priority, dateTimeProvider.UtcNow);
        warehouseRepository.Update(warehouse);
        await cacheService.RemoveByPrefixAsync("warehouses:", ct);

        return ServiceResult.Success();
    }
}