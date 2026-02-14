namespace Application.Discount.Features.Shared;

public class DiscountCodeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? UsageLimit { get; set; }
    public int? MaxUsagePerUser { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RowVersion { get; set; }
}

public class DiscountApplyResultDto
{
    public int DiscountCodeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
}

public class CreateDiscountRestrictionDto
{
    public string RestrictionType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
}

public class DiscountCodeDetailDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? UsageLimit { get; set; }
    public int? MaxUsagePerUser { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RowVersion { get; set; }
    public IEnumerable<DiscountRestrictionDto> Restrictions { get; set; } = Enumerable.Empty<DiscountRestrictionDto>();
    public IEnumerable<DiscountUsageDto> RecentUsages { get; set; } = Enumerable.Empty<DiscountUsageDto>();
}

public class DiscountRestrictionDto
{
    public int Id { get; set; }
    public string RestrictionType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
}

public class DiscountUsageDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime UsedAt { get; set; }
}

public record ValidateDiscountRequest(string Code, decimal OrderTotal);

public record ApplyDiscountRequest(string Code, decimal OrderTotal);