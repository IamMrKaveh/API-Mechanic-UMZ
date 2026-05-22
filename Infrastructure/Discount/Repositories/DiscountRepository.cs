using Domain.Discount.Aggregates;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;

namespace Infrastructure.Discount.Repositories;

public sealed class DiscountRepository(DBContext context) : IDiscountRepository
{
    public async Task<DiscountCode?> GetByIdAsync(DiscountCodeId id, CancellationToken ct = default)
    {
        return await context.DiscountCodes
            .Include(d => d.Restrictions)
            .Include(d => d.Usages)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<DiscountCode?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return await context.DiscountCodes
            .Include(d => d.Restrictions)
            .Include(d => d.Usages)
            .FirstOrDefaultAsync(d => d.Code == normalizedCode, ct);
    }

    public async Task<DiscountCode?> GetByIdWithUsagesAsync(DiscountCodeId id, CancellationToken ct = default)
    {
        return await context.DiscountCodes
            .Include(d => d.Restrictions)
            .Include(d => d.Usages)
                .ThenInclude(u => u.User)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task AddAsync(DiscountCode discountCode, CancellationToken ct = default)
    {
        await context.DiscountCodes.AddAsync(discountCode, ct);
    }

    public void Update(DiscountCode discountCode)
    {
        context.DiscountCodes.Update(discountCode);
    }
}