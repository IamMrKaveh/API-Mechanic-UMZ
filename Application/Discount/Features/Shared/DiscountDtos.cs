namespace Application.Discount.Features.Shared;

public record DiscountCodeDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public decimal Percentage { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public decimal? MinOrderAmount { get; init; }
    public int? UsageLimit { get; init; }
    public int? MaxUsagePerUser { get; init; }
    public int UsedCount { get; init; }
    public bool IsActive { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? StartsAt { get; init; }
    public string? ConcurrencyToken { get; init; }
}

public record DiscountCodeDetailDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public decimal Percentage { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public decimal? MinOrderAmount { get; init; }
    public int? UsageLimit { get; init; }
    public int? MaxUsagePerUser { get; init; }
    public int UsedCount { get; init; }
    public bool IsActive { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? StartsAt { get; init; }
    public string? ConcurrencyToken { get; init; }
    public IEnumerable<DiscountRestrictionDto> Restrictions { get; init; } = Enumerable.Empty<DiscountRestrictionDto>();
    public IEnumerable<DiscountUsageDto> RecentUsages { get; init; } = Enumerable.Empty<DiscountUsageDto>();
}

public record DiscountApplyResultDto
{
    public int DiscountCodeId { get; init; }
    public string Code { get; init; } = string.Empty;
    public decimal DiscountAmount { get; init; }
}

public record CreateDiscountRestrictionDto
{
    public string RestrictionType { get; init; } = string.Empty;
    public int? EntityId { get; init; }
}

public record DiscountRestrictionDto
{
    public int Id { get; init; }
    public string RestrictionType { get; init; } = string.Empty;
    public int? EntityId { get; init; }
}

public record DiscountUsageDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string? UserName { get; init; }
    public DateTime UsedAt { get; init; }
}

public record ValidateDiscountRequest(string Code, decimal OrderTotal);
public record ApplyDiscountRequest(string Code, decimal OrderTotal);