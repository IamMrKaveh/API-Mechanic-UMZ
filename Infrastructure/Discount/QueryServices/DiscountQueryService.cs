using Application.Discount.Contracts;
using Application.Discount.Features.Shared;
using Domain.Discount.Aggregates;
using Domain.Discount.ValueObjects;
using Domain.User.ValueObjects;
using Mapster;
using MapsterMapper;

namespace Infrastructure.Discount.QueryServices;

public sealed class DiscountQueryService(
    DBContext context,
    IMapper mapper) : IDiscountQueryService
{
    public async Task<(IReadOnlyCollection<DiscountCodeDto> Items, int Total)> GetPagedAsync(
        bool includeExpired,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = includeDeleted
            ? context.DiscountCodes.AsNoTracking().IgnoreQueryFilters()
            : context.DiscountCodes.AsNoTracking().AsQueryable();

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

        IReadOnlyCollection<DiscountCodeDto> items = discounts
            .Select(d => new DiscountCodeDto
            {
                Id = d.Id.Value,
                Code = d.Code,
                DiscountType = d.Value.Type.ToString(),
                DiscountValue = d.Value.Amount,
                UsageLimit = d.UsageLimit,
                UsageCount = d.UsageCount,
                IsActive = d.IsActive,
                IsRedeemable = d.IsRedeemable,
                ExpiresAt = d.ExpiresAt,
                CreatedAt = d.CreatedAt
            })
            .ToList()
            .AsReadOnly();

        return (items, total);
    }

    public async Task<DiscountCodeDetailDto?> GetDetailByIdAsync(
        DiscountCodeId id,
        CancellationToken ct = default)
    {
        var discount = await context.DiscountCodes
            .AsNoTracking()
            .Include(d => d.Restrictions)
            .Include(d => d.Usages)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (discount == null)
            return null;

        return new DiscountCodeDetailDto
        {
            Id = discount.Id.Value,
            Code = discount.Code,
            DiscountType = discount.Value.Type.ToString(),
            DiscountValue = discount.Value.Amount,
            MaximumDiscountAmount = discount.MaximumDiscountAmount?.Amount,
            UsageLimit = discount.UsageLimit,
            UsageCount = discount.UsageCount,
            StartsAt = discount.StartsAt,
            ExpiresAt = discount.ExpiresAt,
            IsActive = discount.IsActive,
            IsExpired = discount.IsExpired,
            IsRedeemable = discount.IsRedeemable,
            CreatedAt = discount.CreatedAt,
            Restrictions = discount.Restrictions.Select(r => new DiscountRestrictionDto
            {
                Id = r.Id.Value,
                RestrictionType = r.RestrictionType.ToString(),
                RestrictionValue = r.RestrictionValue
            }).ToList()
        };
    }

    public async Task<DiscountInfoDto?> GetDiscountInfoByCodeAsync(
        string code,
        CancellationToken ct = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        var discount = await context.DiscountCodes
            .AsNoTracking()
            .Include(d => d.Restrictions)
            .FirstOrDefaultAsync(d => d.Code == normalizedCode, ct);

        if (discount == null)
            return null;

        return new DiscountInfoDto
        {
            Code = discount.Code,
            DiscountType = discount.Value.Type.ToString(),
            DiscountValue = discount.Value.Amount,
            MaximumDiscountAmount = discount.MaximumDiscountAmount?.Amount,
            ExpiresAt = discount.ExpiresAt,
            IsRedeemable = discount.IsRedeemable
        };
    }

    public async Task<DiscountValidationResult> ValidateDiscountAsync(
        string code,
        Money orderAmount,
        Guid userId,
        CancellationToken ct = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        var discount = await context.DiscountCodes
            .AsNoTracking()
            .Include(d => d.Restrictions)
            .FirstOrDefaultAsync(d => d.Code == normalizedCode, ct);

        if (discount == null)
            return new DiscountValidationResult
            {
                IsValid = false,
                Error = "کد تخفیف یافت نشد."
            };

        var validation = discount.ValidateForApplication(orderAmount);

        if (!validation.IsValid)
            return new DiscountValidationResult
            {
                IsValid = false,
                Error = validation.FailureReason
            };

        var discountAmount = discount.CalculateDiscount(orderAmount);
        var finalAmount = orderAmount.Subtract(discountAmount);

        return new DiscountValidationResult
        {
            DiscountCodeId = discount.Id.Value,
            Code = discount.Code,
            DiscountType = discount.Value.Type.ToString(),
            DiscountValue = discount.Value.Amount,
            DiscountAmount = discountAmount.Amount,
            FinalAmount = finalAmount.Amount,
            IsValid = true
        };
    }

    public async Task<DiscountUsageReportDto?> GetUsageReportByIdAsync(
        DiscountCodeId id,
        CancellationToken ct = default)
    {
        var discount = await context.DiscountCodes
            .AsNoTracking()
            .Include(d => d.Usages)
                .ThenInclude(u => u.User)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (discount == null)
            return null;

        return new DiscountUsageReportDto
        {
            DiscountCodeId = discount.Id.Value,
            Code = discount.Code,
            UsageLimit = discount.UsageLimit,
            TotalUsages = discount.UsageCount,
            TotalDiscountedAmount = discount.Usages.Sum(u => u.DiscountedAmount),
            Usages = discount.Usages.Select(u => new DiscountUsageItemDto
            {
                UserId = u.UserId.Value,
                OrderId = u.OrderId.Value,
                DiscountedAmount = u.DiscountedAmount,
                UsedAt = u.UsedAt
            }).ToList()
        };
    }
}