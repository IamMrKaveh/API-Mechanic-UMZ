namespace Domain.Discount.Exceptions;

public sealed class DiscountUsageLimitExceededException(string discountCode, int usageLimit, int currentUsage) : DomainException($"کد تخفیف {discountCode} به حداکثر تعداد استفاده ({usageLimit}) رسیده است.")
{
    public string DiscountCode { get; } = discountCode;
    public int UsageLimit { get; } = usageLimit;
    public int CurrentUsage { get; } = currentUsage;
}