using Domain.Discount.Aggregates;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;
using Domain.User.ValueObjects;

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

    public async Task<bool> ExistsByCodeAsync(
        DiscountCode code,
        DiscountCodeId? excludeId = null,
        CancellationToken ct = default)
    {
        var query = context.DiscountCodes.Where(d => d.Code == code.Code);
        if (excludeId is not null)
            query = query.Where(d => d.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task<int> CountUserUsageAsync(
        DiscountCodeId discountId,
        UserId userId,
        CancellationToken ct = default)
    {
        return await context.DiscountCodes
            .Where(d => d.Id == discountId)
            .SelectMany(d => d.Usages)
            .Where(u => u.UserId == userId)
            .CountAsync(ct);
    }

    public async Task AddAsync(DiscountCode discountCode, CancellationToken ct = default)
    {
        await context.DiscountCodes.AddAsync(discountCode, ct);
    }

    public void Update(DiscountCode discountCode)
    {
        context.DiscountCodes.Update(discountCode);
    }

    public void SetOriginalRowVersion(DiscountCode entity, byte[] rowVersion)
    {
        context.Entry(entity).Property<byte[]>("RowVersion").OriginalValue = rowVersion;
    }
}