namespace Infrastructure.Discount.QueryServices;

public class DiscountQueryService(DBContext context, IMapper mapper) : IDiscountQueryService
{
    private readonly DBContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<(IEnumerable<DiscountCodeDto> Discounts, int TotalCount)> GetPagedAsync(
        bool includeExpired,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = includeDeleted
            ? _context.DiscountCodes.AsNoTracking().IgnoreQueryFilters()
            : _context.DiscountCodes.AsNoTracking().AsQueryable();

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

        return (_mapper.Map<IEnumerable<DiscountCodeDto>>(discounts), total);
    }

    public async Task<DiscountCodeDetailDto?> GetDetailByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        var discount = await _context.DiscountCodes
            .AsNoTracking()
            .Include(d => d.Restrictions)
            .Include(d => d.Usages)
                .ThenInclude(u => u.User)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        return discount == null ? null : _mapper.Map<DiscountCodeDetailDto>(discount);
    }

    public async Task<IEnumerable<DiscountCodeDto>> GetActiveDiscountsAsync(
        CancellationToken ct = default)
    {
        var discounts = await _context.DiscountCodes
            .AsNoTracking()
            .Where(d => d.IsActive &&
                        (!d.ExpiresAt.HasValue || d.ExpiresAt > DateTime.UtcNow) &&
                        (!d.StartsAt.HasValue || d.StartsAt <= DateTime.UtcNow))
            .ToListAsync(ct);

        return _mapper.Map<IEnumerable<DiscountCodeDto>>(discounts);
    }

    public async Task<IEnumerable<DiscountCodeDto>> GetExpiringDiscountsAsync(
        DateTime beforeDate,
        CancellationToken ct = default)
    {
        var discounts = await _context.DiscountCodes
            .AsNoTracking()
            .Where(d => d.IsActive && d.ExpiresAt.HasValue && d.ExpiresAt <= beforeDate)
            .ToListAsync(ct);

        return _mapper.Map<IEnumerable<DiscountCodeDto>>(discounts);
    }

    public async Task<DiscountInfoDto?> GetDiscountInfoByCodeAsync(
        string code,
        CancellationToken ct = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        var discount = await _context.DiscountCodes
            .AsNoTracking()
            .Include(d => d.Restrictions)
            .FirstOrDefaultAsync(d => d.Code.Value == normalizedCode, ct);

        if (discount == null)
            return null;

        return new DiscountInfoDto
        {
            Code = discount.Code.Value,
            Percentage = discount.Percentage,
            MaxDiscountAmount = discount.MaxDiscountAmount,
            MinOrderAmount = discount.MinOrderAmount,
            IsActive = discount.IsCurrentlyValid(),
            ExpiresAt = discount.ExpiresAt,
            StartsAt = discount.StartsAt,
            RemainingUsage = discount.RemainingUsage()
        };
    }

    public async Task<DiscountUsageReportDto?> GetUsageReportByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        var discount = await _context.DiscountCodes
            .AsNoTracking()
            .Include(d => d.Usages)
                .ThenInclude(u => u.User)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (discount == null)
            return null;

        return new DiscountUsageReportDto
        {
            DiscountCodeId = discount.Id,
            Code = discount.Code.Value,
            TotalUsageCount = discount.UsageCount,
            UsageLimit = discount.UsageLimit,
            RemainingUsage = discount.RemainingUsage(),
            IsCurrentlyValid = discount.IsCurrentlyValid(),
            Usages = discount.Usages.Select(u => new DiscountUsageItemDto
            {
                Id = u.Id,
                UserId = u.UserId,
                UserName = u.User != null ? $"{u.User.FirstName} {u.User.LastName}" : null,
                OrderId = u.OrderId,
                DiscountAmount = u.DiscountAmount.Amount,
                UsedAt = u.UsedAt,
                IsConfirmed = u.IsConfirmed,
                IsCancelled = u.IsCancelled
            })
        };
    }
}