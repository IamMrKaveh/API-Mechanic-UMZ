namespace Domain.Order.ValueObjects;

public sealed class OrderNumber : ValueObject
{
    public string Value { get; }

    private OrderNumber(string value)
    {
        Value = value;
    }

    public static OrderNumber Generate()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Guid.NewGuid().ToString("N")[..8].ToUpper();
        return new OrderNumber($"ORD-{timestamp}-{random}");
    }

    public static OrderNumber FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("شماره سفارش نمی‌تواند خالی باشد.");

        if (!value.StartsWith("ORD-"))
            throw new DomainException("فرمت شمار�� سفارش نامعتبر است.");

        return new OrderNumber(value.Trim().ToUpper());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToUpperInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(OrderNumber orderNumber) => orderNumber.Value;
}