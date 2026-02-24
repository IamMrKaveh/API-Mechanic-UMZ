namespace Domain.Inventory.ValueObjects;

public sealed class StockQuantity : ValueObject, IComparable<StockQuantity>
{
    public int Value { get; }

    private const int MaxStockValue = 1_000_000;

    private StockQuantity(int value)
    {
        Value = value;
    }

    public static StockQuantity Create(int value)
    {
        if (value < 0)
            throw new DomainException("موجودی نمی‌تواند منفی باشد.");

        if (value > MaxStockValue)
            throw new DomainException($"موجودی نمی‌تواند بیش از {MaxStockValue:N0} باشد.");

        return new StockQuantity(value);
    }

    public static StockQuantity Zero() => new(0);

    public bool IsZero => Value == 0;

    public bool IsPositive => Value > 0;

    public bool IsLowStock(int threshold) => Value > 0 && Value <= threshold;

    public StockQuantity Add(int quantity)
    {
        if (quantity < 0)
            throw new DomainException("مقدار افزایش نمی‌تواند منفی باشد.");

        var newValue = Value + quantity;
        if (newValue > MaxStockValue)
            throw new DomainException($"موجودی نمی‌تواند بیش از {MaxStockValue:N0} باشد.");

        return new StockQuantity(newValue);
    }

    public StockQuantity Subtract(int quantity)
    {
        if (quantity < 0)
            throw new DomainException("مقدار کاهش نمی‌تواند منفی باشد.");

        if (Value < quantity)
            throw new DomainException($"موجودی کافی نیست. موجودی فعلی: {Value}، کاهش درخواستی: {quantity}");

        return new StockQuantity(Value - quantity);
    }

    public (bool CanSubtract, int Shortage) TrySubtract(int quantity)
    {
        if (Value >= quantity)
            return (true, 0);

        return (false, quantity - Value);
    }

    public int CompareTo(StockQuantity? other)
    {
        if (other == null) return 1;
        return Value.CompareTo(other.Value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString("N0");

    public static implicit operator int(StockQuantity quantity) => quantity.Value;

    public static bool operator >(StockQuantity left, StockQuantity right) => left.Value > right.Value;

    public static bool operator <(StockQuantity left, StockQuantity right) => left.Value < right.Value;

    public static bool operator >=(StockQuantity left, StockQuantity right) => left.Value >= right.Value;

    public static bool operator <=(StockQuantity left, StockQuantity right) => left.Value <= right.Value;
}