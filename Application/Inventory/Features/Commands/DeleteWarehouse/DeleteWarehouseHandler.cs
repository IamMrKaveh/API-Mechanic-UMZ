using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;

namespace Application.Inventory.Features.Commands.DeleteWarehouse;

public class DeleteWarehouseHandler(
    IWarehouseRepository warehouseRepository)
    : ICommandHandler<DeleteWarehouseCommand>
{
    public async Task<ServiceResult> Handle(DeleteWarehouseCommand request, CancellationToken ct)
    {
        var id = WarehouseId.From(request.Id);
        var warehouse = await warehouseRepository.GetByIdAsync(id, ct);

        if (warehouse is null)
            return ServiceResult.NotFound("انبار یافت نشد.");

        if (warehouse.IsDefault)
            return ServiceResult.Failure("انبار پیش‌فرض را نمی‌توان حذف کرد.");

        warehouseRepository.Remove(warehouse);

        return ServiceResult.Success();
    }
}