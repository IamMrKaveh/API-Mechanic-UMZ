using Domain.Common.Interfaces;
using Domain.Inventory.Aggregates;
using Domain.Inventory.ValueObjects;

namespace Domain.Inventory.Interfaces;

public interface IWarehouseRepository : IRepository<Warehouse, WarehouseId>
{
    Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken ct = default);

    Task<Warehouse?> GetDefaultAsync(CancellationToken ct = default);

    Task<bool> ExistsByCodeAsync(string code, WarehouseId? excludeId = null, CancellationToken ct = default);

    void Remove(Warehouse warehouse);
}