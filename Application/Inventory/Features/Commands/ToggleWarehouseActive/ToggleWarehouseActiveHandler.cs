using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;

namespace Application.Inventory.Features.Commands.ToggleWarehouseActive;

public class ToggleWarehouseActiveHandler(
    IWarehouseRepository warehouseRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ToggleWarehouseActiveCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ToggleWarehouseActiveCommand request, CancellationToken ct)
    {
        var id = WarehouseId.From(request.Id);
        var warehouse = await warehouseRepository.GetByIdAsync(id, ct);

        if (warehouse is null)
            return ServiceResult.NotFound("انبار یافت نشد.");

        if (request.IsActive)
            warehouse.Activate();
        else
            warehouse.Deactivate();

        warehouseRepository.Update(warehouse);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}