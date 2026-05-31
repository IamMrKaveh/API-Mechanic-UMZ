namespace Application.Discount.Features.Shared;

public record DiscountValidationResult
{
    public Guid DiscountCodeId { get; init; }
    public string Code { get; init; } = string.Empty;
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public string DiscountType { get; init; } = string.Empty;
    public decimal DiscountValue { get; init; }
    public bool IsValid { get; init; }
    public string? Error { get; init; }
}

public record DiscountApplicationResult
{
    public bool IsSuccess { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public string? Error { get; init; }
}

public record DiscountDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string DiscountType { get; init; } = string.Empty;
    public decimal DiscountValue { get; init; }
    public decimal? MaximumDiscountAmount { get; init; }
    public int? UsageLimit { get; init; }
    public int UsageCount { get; init; }
    public DateTime? StartsAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsActive { get; init; }
    public bool IsExpired { get; init; }
    public bool IsRedeemable { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record DiscountCodeDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string DiscountType { get; init; } = string.Empty;
    public decimal DiscountValue { get; init; }
    public int? UsageLimit { get; init; }
    public int UsageCount { get; init; }
    public bool IsActive { get; init; }
    public bool IsRedeemable { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record DiscountCodeDetailDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string DiscountType { get; init; } = string.Empty;
    public decimal DiscountValue { get; init; }
    public decimal? MaximumDiscountAmount { get; init; }
    public int? UsageLimit { get; init; }
    public int UsageCount { get; init; }
    public DateTime? StartsAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsActive { get; init; }
    public bool IsExpired { get; init; }
    public bool IsRedeemable { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<DiscountRestrictionDto> Restrictions { get; init; } = new();
}

public record DiscountRestrictionDto
{
    public Guid Id { get; init; }
    public string RestrictionType { get; init; } = string.Empty;
    public string RestrictionValue { get; init; } = string.Empty;
}

public record DiscountInfoDto
{
    public string Code { get; init; } = string.Empty;
    public string DiscountType { get; init; } = string.Empty;
    public decimal DiscountValue { get; init; }
    public decimal? MaximumDiscountAmount { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsRedeemable { get; init; }
}

public record DiscountUsageReportDto
{
    public Guid DiscountCodeId { get; init; }
    public string Code { get; init; } = string.Empty;
    public int TotalUsages { get; init; }
    public decimal TotalDiscountedAmount { get; init; }
    public int? UsageLimit { get; init; }
    public List<DiscountUsageItemDto> Usages { get; init; } = new();
}

public record DiscountUsageItemDto
{
    public Guid UserId { get; init; }
    public Guid OrderId { get; init; }
    public decimal DiscountedAmount { get; init; }
    public DateTime UsedAt { get; init; }
}