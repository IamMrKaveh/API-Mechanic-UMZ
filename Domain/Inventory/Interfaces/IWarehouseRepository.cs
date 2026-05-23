using Domain.Inventory.Aggregates;
using Domain.Inventory.ValueObjects;

namespace Domain.Inventory.Interfaces;

public interface IWarehouseRepository
{
    Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken ct = default);

    Task<Warehouse?> GetByIdAsync(WarehouseId id, CancellationToken ct = default);

    Task<Warehouse?> GetDefaultAsync(CancellationToken ct = default);

    Task<bool> ExistsByCodeAsync(string code, WarehouseId? excludeId = null, CancellationToken ct = default);

    Task AddAsync(Warehouse warehouse, CancellationToken ct = default);

    void Update(Warehouse warehouse);

    void Remove(Warehouse warehouse);
}