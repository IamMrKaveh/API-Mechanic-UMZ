using Domain.Discount.Aggregates;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Discount.Repositories;

public sealed class DiscountRepository(DBContext context) : IDiscountRepository
{
    public async Task<DiscountCode?> GetByIdAsync(DiscountCodeId id, CancellationToken ct = default)
    {
        return await context.DiscountCodes
            .Include(d => d.Restrictions)
            .Include(d => d.UsageRecords)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<DiscountCode?> GetByCodeAsync(
        DiscountCodeValue code,
        CancellationToken ct = default)
    {
        return await context.DiscountCodes
            .Include(d => d.Restrictions)
            .Include(d => d.UsageRecords)
            .FirstOrDefaultAsync(d => d.Code == code.Value, ct);
    }

    public async Task<bool> ExistsByCodeAsync(
        DiscountCodeValue code,
        DiscountCodeId? excludeId = null,
        CancellationToken ct = default)
    {
        var query = context.DiscountCodes.Where(d => d.Code == code.Value);
        if (excludeId is not null)
            query = query.Where(d => d.Id != excludeId);
        return await query.AnyAsync(ct);
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