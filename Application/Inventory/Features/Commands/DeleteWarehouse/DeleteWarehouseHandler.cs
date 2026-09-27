using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;

namespace Application.Inventory.Features.Commands.DeleteWarehouse;

public class DeleteWarehouseHandler(
    IWarehouseRepository warehouseRepository,
    ICacheService cacheService)
    : ICommandHandler<DeleteWarehouseCommand>
{
    public async Task<ServiceResult> Handle(DeleteWarehouseCommand request, CancellationToken ct)
    {
        var id = WarehouseId.From(request.Id);
        var warehouseResult = await (warehouseRepository.GetByIdAsync(id, ct)).OrNotFoundAsync("انبار یافت نشد.");
        if (warehouseResult.IsFailure) return warehouseResult.Error;
        var warehouse = warehouseResult.Value;

        if (warehouse.IsDefault)
            return ServiceResult.Failure("انبار پیش‌فرض را نمی‌توان حذف کرد.");

        warehouseRepository.Remove(warehouse);
        await cacheService.RemoveByPrefixAsync("warehouses:", ct);

        return ServiceResult.Success();
    }
}