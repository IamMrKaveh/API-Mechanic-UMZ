namespace Infrastructure.Discount.Repositories;

public class DiscountRepository : IDiscountRepository
{
    private readonly LedkaContext _context;

    public DiscountRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<DiscountCode?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.DiscountCodes
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<DiscountCode?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _context.DiscountCodes
            .Include(d => d.Restrictions)
            .FirstOrDefaultAsync(d => d.Code == code, ct);
    }

    /// <summary>
    /// SELECT FOR UPDATE - قفل‌گذاری سطری برای جلوگیری از Race Condition
    /// در PostgreSQL از Raw SQL استفاده می‌شود
    /// </summary>
    public async Task<DiscountCode?> GetByCodeForUpdateAsync(string code, CancellationToken ct = default)
    {
        // PostgreSQL: SELECT ... FOR UPDATE
        var discount = await _context.DiscountCodes
            .FromSqlInterpolated(
                $"SELECT * FROM \"DiscountCodes\" WHERE \"Code\" = {code} AND \"IsDeleted\" = false FOR UPDATE")
            .FirstOrDefaultAsync(ct);

        return discount;
    }

    public async Task<DiscountCode?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default)
    {
        return await _context.DiscountCodes
            .Include(d => d.Restrictions)
            .Include(d => d.Usages)
                .ThenInclude(u => u.User)
            .IgnoreQueryFilters() // برای نمایش در پنل ادمین حتی اگر حذف نرم شده باشد
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<DiscountCode?> GetByIdWithUsagesAsync(int id, CancellationToken ct = default)
    {
        return await _context.DiscountCodes
            .Include(d => d.Usages)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<bool> ExistsByCodeAsync(string code, int? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.DiscountCodes.Where(d => d.Code == code);

        if (excludeId.HasValue)
            query = query.Where(d => d.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<(IEnumerable<DiscountCode> Discounts, int TotalCount)> GetPagedAsync(
        bool includeExpired,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = includeDeleted
            ? _context.DiscountCodes.IgnoreQueryFilters()
            : _context.DiscountCodes.AsQueryable();

        if (!includeExpired)
        {
            query = query.Where(d =>
                !d.ExpiresAt.HasValue || d.ExpiresAt > DateTime.UtcNow);
        }

        var total = await query.CountAsync(ct);

        var discounts = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (discounts, total);
    }

    public async Task<int> CountUserUsageAsync(int discountId, int userId, CancellationToken ct = default)
    {
        return await _context.DiscountUsages
            .Where(u => u.DiscountCodeId == discountId && u.UserId == userId && !u.IsCancelled)
            .CountAsync(ct);
    }

    public async Task<IEnumerable<DiscountCode>> GetActiveDiscountsAsync(CancellationToken ct = default)
    {
        return await _context.DiscountCodes
            .Where(d => d.IsActive &&
                        (!d.ExpiresAt.HasValue || d.ExpiresAt > DateTime.UtcNow) &&
                        (!d.StartsAt.HasValue || d.StartsAt <= DateTime.UtcNow))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<DiscountCode>> GetExpiringDiscountsAsync(DateTime beforeDate, CancellationToken ct = default)
    {
        return await _context.DiscountCodes
            .Where(d => d.IsActive && d.ExpiresAt.HasValue && d.ExpiresAt <= beforeDate)
            .ToListAsync(ct);
    }

    public async Task<DiscountUsage?> GetUsageByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        return await _context.DiscountUsages
            .Include(u => u.DiscountCode)
            .FirstOrDefaultAsync(u => u.OrderId == orderId, ct);
    }

    public async Task AddAsync(DiscountCode discount, CancellationToken ct = default)
    {
        await _context.DiscountCodes.AddAsync(discount, ct);
    }

    public void Update(DiscountCode discount)
    {
        _context.DiscountCodes.Update(discount);
    }

    public void SetOriginalRowVersion(DiscountCode entity, byte[] rowVersion)
    {
        _context.Entry(entity).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }
}