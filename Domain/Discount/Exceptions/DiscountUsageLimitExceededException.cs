namespace Domain.Discount.Exceptions;

public sealed class DiscountUsageLimitExceededException : DomainException
{
    public string DiscountCode { get; }
    public int UsageLimit { get; }
    public int CurrentUsage { get; }

    public DiscountUsageLimitExceededException(string discountCode, int usageLimit, int currentUsage)
        : base($"کد تخفیف {discountCode} به حداکثر تعداد استفاده ({usageLimit}) رسیده است.")
    {
        DiscountCode = discountCode;
        UsageLimit = usageLimit;
        CurrentUsage = currentUsage;
    }
}