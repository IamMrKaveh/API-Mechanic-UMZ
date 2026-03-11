using Domain.Common.Abstractions;
using Domain.Discount.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Discount.Entities;

public sealed class DiscountUsageRecord : Entity<DiscountUsageId>
{
    private DiscountUsageRecord()
    { }

    public DiscountCodeId DiscountCodeId { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public UserId UserId { get; private set; } = default!;
    public string OrderId { get; private set; } = default!;
    public decimal DiscountedAmount { get; private set; }
    public int UsageCountAtTime { get; private set; }
    public DateTime UsedAt { get; private set; }

    internal static DiscountUsageRecord Create(
        DiscountCodeId discountCodeId,
        string code,
        UserId userId,
        string orderId,
        decimal discountedAmount,
        int usageCountAtTime)
    {
        return new DiscountUsageRecord
        {
            Id = DiscountUsageId.NewId(),
            DiscountCodeId = discountCodeId,
            Code = code,
            UserId = userId,
            OrderId = orderId,
            DiscountedAmount = discountedAmount,
            UsageCountAtTime = usageCountAtTime,
            UsedAt = DateTime.UtcNow
        };
    }
}