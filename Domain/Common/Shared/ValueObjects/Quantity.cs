namespace Domain.Common.Shared.ValueObjects;

public class Quantity : ValueObject
{
    public int Value { get; private set; }

    private Quantity()
    { }

    private Quantity(int value)
    {
        if (value < 0) throw new DomainException("Quantity cannot be negative.");
        Value = value;
    }

    public static Quantity Create(int value) => new(value);

    public Quantity Add(Quantity other) => new(Value + other.Value);

    public Quantity Subtract(Quantity other)
    {
        if (Value < other.Value) throw new DomainException("Insufficient quantity.");
        return new(Value - other.Value);
    }

    public static implicit operator int(Quantity q) => q.Value;

    public static implicit operator Quantity(int value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}