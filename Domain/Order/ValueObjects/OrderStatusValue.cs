namespace Domain.Order.ValueObjects;

public sealed record OrderStatusValue
{
    public string Value { get; }
    public string DisplayName { get; }
    public int SortOrder { get; }
    public bool IsFinal { get; }

    private OrderStatusValue(string value, string displayName, int sortOrder, bool isFinal)
    {
        Value = value;
        DisplayName = displayName;
        SortOrder = sortOrder;
        IsFinal = isFinal;
    }

    public static readonly OrderStatusValue Created = new("Created", "ایجاد شده", 0, false);
    public static readonly OrderStatusValue Reserved = new("Reserved", "رزرو شده", 1, false);
    public static readonly OrderStatusValue Pending = new("Pending", "در انتظار پرداخت", 2, false);
    public static readonly OrderStatusValue Failed = new("Failed", "ناموفق", 3, false);
    public static readonly OrderStatusValue Paid = new("Paid", "پرداخت شده", 4, false);
    public static readonly OrderStatusValue Processing = new("Processing", "در حال پردازش", 5, false);
    public static readonly OrderStatusValue Shipped = new("Shipped", "ارسال شده", 6, false);
    public static readonly OrderStatusValue Delivered = new("Delivered", "تحویل داده شده", 7, true);
    public static readonly OrderStatusValue Cancelled = new("Cancelled", "لغو شده", 8, true);
    public static readonly OrderStatusValue Returned = new("Returned", "برگشت خورده", 9, true);
    public static readonly OrderStatusValue Refunded = new("Refunded", "استرداد شده", 10, true);
    public static readonly OrderStatusValue Expired = new("Expired", "منقضی شده", 11, true);

    private static readonly IReadOnlyDictionary<string, OrderStatusValue> All =
        new Dictionary<string, OrderStatusValue>(StringComparer.OrdinalIgnoreCase)
        {
            [Created.Value] = Created,
            [Reserved.Value] = Reserved,
            [Pending.Value] = Pending,
            [Failed.Value] = Failed,
            [Paid.Value] = Paid,
            [Processing.Value] = Processing,
            [Shipped.Value] = Shipped,
            [Delivered.Value] = Delivered,
            [Cancelled.Value] = Cancelled,
            [Returned.Value] = Returned,
            [Refunded.Value] = Refunded,
            [Expired.Value] = Expired
        };

    private static readonly IReadOnlyDictionary<OrderStatusValue, IReadOnlySet<OrderStatusValue>> Transitions =
        new Dictionary<OrderStatusValue, IReadOnlySet<OrderStatusValue>>
        {
            [Created] = new HashSet<OrderStatusValue> { Reserved, Cancelled, Expired },
            [Reserved] = new HashSet<OrderStatusValue> { Pending, Cancelled, Expired },
            [Pending] = new HashSet<OrderStatusValue> { Paid, Failed, Cancelled, Expired },
            [Failed] = new HashSet<OrderStatusValue> { Pending, Cancelled, Expired },
            [Paid] = new HashSet<OrderStatusValue> { Processing, Cancelled, Refunded },
            [Processing] = new HashSet<OrderStatusValue> { Shipped, Cancelled },
            [Shipped] = new HashSet<OrderStatusValue> { Delivered, Returned },
            [Delivered] = new HashSet<OrderStatusValue> { Returned, Refunded },
            [Returned] = new HashSet<OrderStatusValue> { Refunded },
            [Cancelled] = new HashSet<OrderStatusValue>(),
            [Refunded] = new HashSet<OrderStatusValue>(),
            [Expired] = new HashSet<OrderStatusValue>()
        };

    public bool IsPaid => this == Paid || this == Processing || this == Shipped || this == Delivered;

    public static OrderStatusValue From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Invalid order status.", nameof(value));

        if (All.TryGetValue(value, out var status))
            return status;

        throw new ArgumentException($"'{value}' is not a valid order status.", nameof(value));
    }

    public bool CanTransitionTo(OrderStatusValue next)
    {
        return Transitions.TryGetValue(this, out var allowed) && allowed.Contains(next);
    }

    public bool CanBeCancelled()
    {
        return !IsFinal && this != Shipped;
    }

    public bool CanBeEdited()
    {
        return this == Created || this == Reserved || this == Pending;
    }

    public static implicit operator string(OrderStatusValue status)
    {
        return status.Value;
    }
}