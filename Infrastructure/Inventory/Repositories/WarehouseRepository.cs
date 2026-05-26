using Domain.Inventory.Aggregates;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;

namespace Infrastructure.Inventory.Repositories;

public sealed class WarehouseRepository(DBContext context) : IWarehouseRepository
{
    public async Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Warehouses
            .AsNoTracking()
            .OrderBy(w => w.Priority)
            .ToListAsync(ct);
    }

    public async Task<Warehouse?> GetByIdAsync(WarehouseId id, CancellationToken ct = default)
    {
        return await context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public async Task<Warehouse?> GetDefaultAsync(CancellationToken ct = default)
    {
        return await context.Warehouses
            .FirstOrDefaultAsync(w => w.IsDefault, ct);
    }

    public async Task<bool> ExistsByCodeAsync(string code, WarehouseId? excludeId = null, CancellationToken ct = default)
    {
        var query = context.Warehouses.Where(w => w.Code.Value == code);
        if (excludeId is not null)
            query = query.Where(w => w.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(Warehouse warehouse, CancellationToken ct = default)
    {
        await context.Warehouses.AddAsync(warehouse, ct);
    }

    public void Update(Warehouse warehouse)
    {
        context.Warehouses.Update(warehouse);
    }

    public void Remove(Warehouse warehouse)
    {
        context.Warehouses.Remove(warehouse);
    }
}