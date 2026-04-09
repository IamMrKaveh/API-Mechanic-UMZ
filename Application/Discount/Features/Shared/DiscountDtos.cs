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

public record DiscountDetailDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string DiscountType { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public decimal? MinOrderAmount { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public int? UsageLimit { get; init; }
    public int UsageCount { get; init; }
    public bool IsActive { get; init; }
    public DateTime? StartsAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? RowVersion { get; init; }
}

public record DiscountListItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string DiscountType { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public int? UsageLimit { get; init; }
    public int UsageCount { get; init; }
    public bool IsActive { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
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

public record DiscountApplicationResult
{
    public bool IsSuccess { get; init; }
    public decimal DiscountAmount { get; init; }
    public string? Error { get; init; }
}