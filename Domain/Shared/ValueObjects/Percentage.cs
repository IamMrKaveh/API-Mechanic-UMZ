namespace Domain.Shared.ValueObjects;

public sealed class Percentage : IEquatable<Percentage>
{
    public decimal Value { get; }

    private Percentage(decimal value)
    {
        Value = value;
    }

    public static Percentage Create(decimal value)
    {
        if (value < 0 || value > 100)
            throw new DomainException("درصد باید بین ۰ تا ۱۰۰ باشد.");

        return new Percentage(value);
    }

    public static Percentage Zero => new(0);
    public static Percentage Full => new(100);

    public decimal ApplyTo(decimal amount) => amount * Value / 100;

    public bool Equals(Percentage? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as Percentage);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(Percentage? left, Percentage? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Percentage? left, Percentage? right) => !(left == right);

    public override string ToString() => $"{Value}%";
}