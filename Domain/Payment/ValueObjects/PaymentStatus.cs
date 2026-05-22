namespace Domain.Payment.ValueObjects;

public sealed class PaymentStatus : ValueObject
{
    public string Value { get; }
    public string DisplayName { get; }
    public int Order { get; }
    public bool IsFinal { get; }

    private PaymentStatus(string value, string displayName, int order, bool isFinal)
    {
        Value = value;
        DisplayName = displayName;
        Order = order;
        IsFinal = isFinal;
    }

    public static PaymentStatus Pending => new("Pending", "در انتظار پرداخت", 0, false);
    public static PaymentStatus Processing => new("Processing", "در حال پردازش", 1, false);
    public static PaymentStatus Success => new("Success", "موفق", 2, true);
    public static PaymentStatus Failed => new("Failed", "ناموفق", 3, true);
    public static PaymentStatus Expired => new("Expired", "منقضی شده", 4, true);
    public static PaymentStatus Cancelled => new("Cancelled", "لغو شده", 5, true);
    public static PaymentStatus Refunded => new("Refunded", "بازگشت داده شده", 6, true);

    public static PaymentStatus FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Pending;

        return value.ToLowerInvariant() switch
        {
            "pending" => Pending,
            "processing" => Processing,
            "success" => Success,
            "failed" => Failed,
            "expired" => Expired,
            "cancelled" => Cancelled,
            "refunded" => Refunded,
            _ => throw new DomainException($"وضعیت پرداخت '{value}' نامعتبر است.")
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => DisplayName;

    public static implicit operator string(PaymentStatus status) => status.Value;

    public static implicit operator int(PaymentStatus status) => status.Order;
}