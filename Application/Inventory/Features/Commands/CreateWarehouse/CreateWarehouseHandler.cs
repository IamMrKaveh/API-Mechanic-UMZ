using Domain.Inventory.Aggregates;
using Domain.Inventory.Interfaces;

namespace Application.Inventory.Features.Commands.CreateWarehouse;

public class CreateWarehouseHandler(
    IWarehouseRepository warehouseRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateWarehouseCommand>
{
    public async Task<ServiceResult> Handle(CreateWarehouseCommand request, CancellationToken ct)
    {
        var codeExists = await warehouseRepository.ExistsByCodeAsync(request.Code, null, ct);
        if (codeExists)
            return ServiceResult.Conflict("کد انبار تکراری است.");

        if (request.IsDefault)
        {
            var current = await warehouseRepository.GetDefaultAsync(ct);
            if (current is not null)
            {
                current.ClearDefault();
                warehouseRepository.Update(current);
            }
        }

        var warehouse = Warehouse.Create(
            request.Code,
            request.Name,
            request.City,
            request.Address,
            request.Phone,
            request.Priority,
            request.IsDefault);

        await warehouseRepository.AddAsync(warehouse, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}