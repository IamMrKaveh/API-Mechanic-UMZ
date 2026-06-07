namespace Domain.Order.ValueObjects;

public sealed record OrderNumber
{
    public string Value { get; }

    private OrderNumber(string value) => Value = value;

    public static OrderNumber Generate(DateOnly date)
    {
        var datePart = date.ToString("yyyyMMdd");
        var uniquePart = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return new OrderNumber($"ORD-{datePart}-{uniquePart}");
    }

    public static OrderNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Order number cannot be empty.");
        return new OrderNumber(value.Trim().ToUpperInvariant());
    }

    public override string ToString() => Value;
}