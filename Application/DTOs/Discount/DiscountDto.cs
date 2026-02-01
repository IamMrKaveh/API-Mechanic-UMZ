namespace Application.DTOs.Discount;

public class DiscountCodeDto { public int Id { get; set; } public string Code { get; set; } = string.Empty; public decimal Percentage { get; set; } public decimal? MaxDiscountAmount { get; set; } public decimal? MinOrderAmount { get; set; } public int? UsageLimit { get; set; } public int UsedCount { get; set; } public bool IsActive { get; set; } public DateTime? ExpiresAt { get; set; } public DateTime CreatedAt { get; set; } public string? RowVersion { get; set; } }

public class DiscountCodeDetailDto : DiscountCodeDto { public IEnumerable<DiscountRestrictionDto> Restrictions { get; set; } = []; public IEnumerable<DiscountUsageDto> RecentUsages { get; set; } = []; }

public class DiscountRestrictionDto { public int Id { get; set; } public string RestrictionType { get; set; } = string.Empty; public int? EntityId { get; set; } }

public class DiscountUsageDto { public int Id { get; set; } public int UserId { get; set; } public string? UserName { get; set; } public int OrderId { get; set; } public decimal DiscountAmount { get; set; } public DateTime UsedAt { get; set; } public bool IsConfirmed { get; set; } }

public class DiscountApplyResultDto { public decimal DiscountAmount { get; set; } public int DiscountCodeId { get; set; } }

public record ApplyDiscountDto(string Code, decimal OrderTotal);

public class CreateDiscountDto { public required string Code { get; set; } public decimal Percentage { get; set; } public decimal? MaxDiscountAmount { get; set; } public decimal? MinOrderAmount { get; set; } public int? UsageLimit { get; set; } public DateTime? ExpiresAt { get; set; } public List<CreateDiscountRestrictionDto>? Restrictions { get; set; } }

public class CreateDiscountRestrictionDto { public required string RestrictionType { get; set; } public int? EntityId { get; set; } }

public class UpdateDiscountDto { public decimal? Percentage { get; set; } public decimal? MaxDiscountAmount { get; set; } public decimal? MinOrderAmount { get; set; } public int? UsageLimit { get; set; } public bool? IsActive { get; set; } public DateTime? ExpiresAt { get; set; } public string? RowVersion { get; set; } }