namespace Presentation.Discount.Requests;

public record CreateDiscountRequest(
    string Code,
    string DiscountType,
    decimal DiscountValue,
    decimal? MaximumDiscountAmount,
    int? UsageLimit,
    DateTime? StartsAt,
    DateTime? ExpiresAt
);

public record UpdateDiscountRequest(
    string DiscountType,
    decimal DiscountValue,
    decimal? MaximumDiscountAmount,
    int? UsageLimit,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    bool IsActive
);

public record ValidateDiscountRequest(string Code, decimal OrderAmount);

public record ApplyDiscountRequest(string Code, Guid OrderId, decimal OrderAmount);

public record CancelDiscountUsageRequest(Guid OrderId);