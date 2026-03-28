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

public record DiscountCodeDetailDto(
    int Id,
    string Code,
    decimal Percentage,
    decimal? MaxDiscountAmount,
    decimal? MinOrderAmount,
    int? UsageLimit,
    int? MaxUsagePerUser,
    int UsedCount,
    bool IsActive,
    DateTime? ExpiresAt,
    DateTime? StartsAt,
    string? ConcurrencyToken,
    IEnumerable<DiscountRestrictionDto> Restrictions,
    IEnumerable<DiscountUsageDto> RecentUsages
);

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

public record DiscountRestrictionDto(
    int Id,
    string RestrictionType,
    int? EntityId
);

public record DiscountUsageDto(
    int Id,
    int UserId,
    string? UserName,
    DateTime UsedAt
);

public record DiscountInfoDto
{
    public string Code { get; init; } = string.Empty;
    public decimal Percentage { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public decimal? MinOrderAmount { get; init; }
    public bool IsActive { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? StartsAt { get; init; }
    public int RemainingUsage { get; init; }
}

public record DiscountUsageReportDto
{
    public int DiscountCodeId { get; init; }
    public string Code { get; init; } = string.Empty;
    public int TotalUsageCount { get; init; }
    public int? UsageLimit { get; init; }
    public int RemainingUsage { get; init; }
    public bool IsCurrentlyValid { get; init; }
    public IEnumerable<DiscountUsageItemDto> Usages { get; init; } = [];
}

public record DiscountUsageItemDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string? UserName { get; init; }
    public int OrderId { get; init; }
    public decimal DiscountAmount { get; init; }
    public DateTime UsedAt { get; init; }
    public bool IsConfirmed { get; init; }
    public bool IsCancelled { get; init; }
}

public record DiscountValidationDto(
    bool IsValid,
    decimal EstimatedDiscount,
    string? Message
);