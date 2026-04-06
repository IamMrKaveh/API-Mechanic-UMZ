namespace Presentation.Discount.Requests;

public record ValidateDiscountRequest(string Code, decimal OrderTotal);

public record ApplyDiscountRequest(string Code, decimal OrderTotal);

public record CancelDiscountUsageRequest(int OrderId);