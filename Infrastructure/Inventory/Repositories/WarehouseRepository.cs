using Domain.Inventory.Aggregates;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;

namespace Infrastructure.Inventory.Repositories;

public sealed class WarehouseRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<Warehouse, WarehouseId>(context), IWarehouseRepository
{
    public async Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken ct = default)
    {
        return await Context.Warehouses
            .AsNoTracking()
            .OrderBy(w => w.Priority)
            .ToListAsync(ct);
    }

    public async Task<Warehouse?> GetDefaultAsync(CancellationToken ct = default)
    {
        return await Context.Warehouses
            .FirstOrDefaultAsync(w => w.IsDefault, ct);
    }

    public async Task<bool> ExistsByCodeAsync(string code, WarehouseId? excludeId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var codeVo = WarehouseCode.Create(code);

        var query = Context.Warehouses.Where(w => w.Code == codeVo);
        if (excludeId is not null)
            query = query.Where(w => w.Id != excludeId);

        return await query.AnyAsync(ct);
    }

    public void Remove(Warehouse warehouse) => Context.Warehouses.Remove(warehouse);
}
