using Domain.Discount.Events;
using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Discount.Aggregates;

public sealed class DiscountUsage : AggregateRoot<DiscountUsageId>
{
    private DiscountUsage()
    { }

    public DiscountCodeId DiscountCodeId { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public UserId UserId { get; private set; } = default!;
    public OrderId OrderId { get; private set; } = default!;
    public decimal DiscountedAmount { get; private set; }
    public int UsageCountAtTime { get; private set; }
    public DateTime UsedAt { get; private set; }

    public static DiscountUsage Record(
        DiscountUsageId id,
        DiscountCodeId discountCodeId,
        string code,
        UserId userId,
        OrderId orderId,
        decimal discountedAmount,
        int usageCountAtTime)
    {
        var usage = new DiscountUsage
        {
            Id = id,
            DiscountCodeId = discountCodeId,
            Code = code,
            UserId = userId,
            OrderId = orderId,
            DiscountedAmount = discountedAmount,
            UsageCountAtTime = usageCountAtTime,
            UsedAt = DateTime.UtcNow
        };

        usage.RaiseDomainEvent(new DiscountUsageRecordedEvent(id, discountCodeId, userId, orderId, discountedAmount));
        return usage;
    }
}