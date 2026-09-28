using Domain.Discount.Aggregates;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;

namespace Infrastructure.Discount.Repositories;

public sealed class DiscountRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<DiscountCode, DiscountCodeId>(context), IDiscountRepository
{
    public override async Task<DiscountCode?> GetByIdAsync(DiscountCodeId id, CancellationToken ct = default)
    {
        return await Context.DiscountCodes
            .Include(d => d.Restrictions)
            .Include(d => d.Usages)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<DiscountCode?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return await Context.DiscountCodes
            .Include(d => d.Restrictions)
            .Include(d => d.Usages)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Code == normalizedCode, ct);
    }

    public async Task<DiscountCode?> GetByIdWithUsagesAsync(DiscountCodeId id, CancellationToken ct = default)
    {
        return await Context.DiscountCodes
            .Include(d => d.Restrictions)
            .Include(d => d.Usages)
                .ThenInclude(u => u.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

}