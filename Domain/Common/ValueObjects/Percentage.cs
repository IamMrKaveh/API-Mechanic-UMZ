namespace Domain.Common.ValueObjects;

public sealed record Percentage
{
    public decimal Value { get; }

    private Percentage(decimal value) => Value = value;

    public static Percentage Create(decimal value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentException("Percentage must be between 0 and 100.", nameof(value));
        return new Percentage(value);
    }

    public static Percentage Zero => new(0);
    public static Percentage Hundred => new(100);

    public decimal ApplyTo(decimal amount) => decimal.Round(amount * (Value / 100), 2);

    public Money ApplyTo(Money money) => money.Multiply(Value / 100);

    public override string ToString() => $"{Value:N2}%";
}