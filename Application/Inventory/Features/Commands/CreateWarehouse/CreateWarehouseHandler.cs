using Domain.Inventory.Aggregates;
using Domain.Inventory.Interfaces;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Inventory.Features.Commands.CreateWarehouse;

public class CreateWarehouseHandler(
    IWarehouseRepository warehouseRepository,
    ICacheService cacheService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateWarehouseCommand>
{
    public async Task<ServiceResult> Handle(CreateWarehouseCommand request, CancellationToken ct)
    {
        var codeExists = await warehouseRepository.ExistsByCodeAsync(request.Code, null, ct);
        if (codeExists)
            return ServiceResult.Conflict("کد انبار تکراری است.");

        var now = dateTimeProvider.UtcNow;

        if (request.IsDefault)
        {
            var current = await warehouseRepository.GetDefaultAsync(ct);
            if (current is not null)
            {
                current.ClearDefault(now);
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
            now,
            request.IsDefault);

        await warehouseRepository.AddAsync(warehouse, ct);
        await cacheService.RemoveByPrefixAsync("warehouses:", ct);

        return ServiceResult.Success();
    }
}