namespace Domain.Order.ValueObjects;

/// <summary>
/// Value Object برای وضعیت سفارش با State Machine داخلی
/// </summary>
public sealed class OrderStatusValue : ValueObject
{
    public string Value { get; }
    public string DisplayName { get; }
    public int SortOrder { get; }
    public bool IsFinal { get; }

    public static readonly OrderStatusValue Created
    = new("Created", "ایجاد شده", -1, false);

    public static readonly OrderStatusValue Reserved
        = new("Reserved", "رزرو شده", 0, false);

    public static readonly OrderStatusValue Failed
        = new("Failed", "ناموفق", 8, true);

    public static readonly OrderStatusValue Expired
        = new("Expired", "منقضی شده", 9, true);

    private OrderStatusValue(string value, string displayName, int sortOrder, bool isFinal)
    {
        Value = value;
        DisplayName = displayName;
        SortOrder = sortOrder;
        IsFinal = isFinal;
    }

    #region Predefined Statuses

    public static OrderStatusValue Pending => new("Pending", "در انتظار پرداخت", 0, false);
    public static OrderStatusValue Paid => new("Paid", "پرداخت شده", 1, false);
    public static OrderStatusValue Processing => new("Processing", "در حال پردازش", 2, false);
    public static OrderStatusValue Shipped => new("Shipped", "ارسال شده", 3, false);
    public static OrderStatusValue Delivered => new("Delivered", "تحویل داده شده", 4, true);
    public static OrderStatusValue Cancelled => new("Cancelled", "لغو شده", 5, true);
    public static OrderStatusValue Returned => new("Returned", "برگشت خورده", 6, true);
    public static OrderStatusValue Refunded => new("Refunded", "استرداد شده", 7, true);

    #endregion Predefined Statuses

    #region State Machine - Transition Rules

    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new()
    {
        { "Created", new HashSet<string> { "Reserved", "Cancelled", "Expired" } },
        { "Reserved", new HashSet<string> { "Pending", "Cancelled", "Expired" } },

        { "Pending", new HashSet<string> { "Paid", "Failed", "Cancelled", "Expired" } },
        { "Paid", new HashSet<string> { "Processing", "Cancelled", "Refunded" } },
        { "Processing", new HashSet<string> { "Shipped", "Cancelled" } },
        { "Shipped", new HashSet<string> { "Delivered", "Returned" } },
        { "Delivered", new HashSet<string> { "Returned", "Refunded" } },

        { "Failed", new HashSet<string>() },
        { "Expired", new HashSet<string>() },
        { "Cancelled", new HashSet<string>() },
        { "Refunded", new HashSet<string>() },
        { "Returned", new HashSet<string> { "Refunded" } }
    };

    public bool CanTransitionTo(OrderStatusValue newStatus)
    {
        if (newStatus == null) return false;
        if (Value == newStatus.Value) return false;

        return AllowedTransitions.TryGetValue(Value, out var allowed) &&
               allowed.Contains(newStatus.Value);
    }

    public IEnumerable<OrderStatusValue> GetAllowedNextStatuses()
    {
        if (!AllowedTransitions.TryGetValue(Value, out var allowed))
            return Enumerable.Empty<OrderStatusValue>();

        return allowed.Select(FromString);
    }

    #endregion State Machine - Transition Rules

    #region Query Methods

    public bool IsPending() => Value == Pending.Value;

    public bool IsPaid() => Value == Paid.Value;

    public bool IsProcessing() => Value == Processing.Value;

    public bool IsShipped() => Value == Shipped.Value;

    public bool IsDelivered() => Value == Delivered.Value;

    public bool IsCancelled() => Value == Cancelled.Value;

    public bool IsReturned() => Value == Returned.Value;

    public bool IsRefunded() => Value == Refunded.Value;

    public bool RequiresPayment() => Value == Pending.Value;

    public bool CanBeEdited() => Value == Pending.Value;

    public bool CanBeCancelled() => !IsFinal && Value != Shipped.Value;

    public bool CanBeShipped() => Value == Processing.Value;

    public bool CanBeDelivered() => Value == Shipped.Value;

    #endregion Query Methods

    #region Factory Methods

    public static OrderStatusValue FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Pending;

        return value.ToLowerInvariant() switch
        {
            "created" => Created,
            "reserved" => Reserved,
            "failed" => Failed,
            "expired" => Expired,

            "pending" => Pending,
            "paid" => Paid,
            "processing" => Processing,
            "shipped" => Shipped,
            "delivered" => Delivered,
            "cancelled" => Cancelled,
            "returned" => Returned,
            "refunded" => Refunded,
            _ => throw new DomainException($"وضعیت سفارش '{value}' نامعتبر است.")
        };
    }

    public static IEnumerable<OrderStatusValue> GetAll()
    {
        yield return Created;
        yield return Reserved;

        yield return Pending;
        yield return Paid;
        yield return Processing;
        yield return Shipped;
        yield return Delivered;

        yield return Cancelled;
        yield return Returned;
        yield return Refunded;
        yield return Failed;
        yield return Expired;
    }

    public static IEnumerable<OrderStatusValue> GetActiveStatuses()
    {
        return GetAll().Where(s => !s.IsFinal);
    }

    public static IEnumerable<OrderStatusValue> GetFinalStatuses()
    {
        return GetAll().Where(s => s.IsFinal);
    }

    public bool IsCreated() => Value == Created.Value;

    public bool IsReserved() => Value == Reserved.Value;

    public bool IsFailed() => Value == Failed.Value;

    public bool IsExpired() => Value == Expired.Value;

    #endregion Factory Methods

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => DisplayName;

    public static implicit operator string(OrderStatusValue status) => status.Value;

    public static bool operator ==(OrderStatusValue? left, OrderStatusValue? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Value == right.Value;
    }

    public static bool operator !=(OrderStatusValue? left, OrderStatusValue? right) => !(left == right);

    public override bool Equals(object? obj)
    {
        if (obj is OrderStatusValue other)
            return Value == other.Value;
        return false;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}