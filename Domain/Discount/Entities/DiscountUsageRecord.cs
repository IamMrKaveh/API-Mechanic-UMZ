using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Discount.Entities;

public sealed class DiscountUsageRecord : Entity<DiscountUsageId>
{
    private DiscountUsageRecord()
    { }

    public string Code { get; private set; } = default!;
    public decimal DiscountedAmount { get; private set; }
    public int UsageCountAtTime { get; private set; }
    public DateTime UsedAt { get; private set; }

    public DiscountCodeId DiscountCodeId { get; private set; } = default!;
    public UserId UserId { get; private set; } = default!;
    public User.Aggregates.User User { get; private set; } = default!;
    public OrderId OrderId { get; private set; } = default!;
    public Order.Aggregates.Order Order { get; private set; } = default!;

    internal static DiscountUsageRecord Create(
        DiscountCodeId discountCodeId,
        string code,
        UserId userId,
        OrderId orderId,
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