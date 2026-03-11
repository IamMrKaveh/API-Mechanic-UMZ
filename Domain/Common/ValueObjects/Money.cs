namespace Domain.Common.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "IRR")
    {
        if (amount < 0)
            throw new ArgumentException("Money amount cannot be negative.", nameof(amount));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty.", nameof(currency));
        return new Money(amount, currency.ToUpperInvariant().Trim());
    }

    public static Money FromDecimal(decimal amount, string currency = "IRR") => Create(amount, currency);

    public static Money Zero(string currency = "IRR") => new(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        if (Amount < other.Amount)
            throw new InvalidOperationException("Insufficient amount for subtraction.");
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new ArgumentException("Factor cannot be negative.", nameof(factor));
        return new Money(decimal.Round(Amount * factor, 2), Currency);
    }

    public Money Multiply(int factor)
    {
        if (factor < 0)
            throw new ArgumentException("Factor cannot be negative.", nameof(factor));
        return new Money(decimal.Round(Amount * factor, 2), Currency);
    }

    public bool IsGreaterThan(Money other)
    {
        EnsureSameCurrency(other);
        return Amount > other.Amount;
    }

    public bool IsGreaterThanOrEqual(Money other)
    {
        EnsureSameCurrency(other);
        return Amount >= other.Amount;
    }

    public bool IsZero => Amount == 0;

    public string ToTomanString() => $"{Amount:N0} تومان";

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static bool operator >(Money left, Money right) => left.IsGreaterThan(right);

    public static bool operator <(Money left, Money right) => right.IsGreaterThan(left);

    public static bool operator >=(Money left, Money right) => left.IsGreaterThanOrEqual(right);

    public static bool operator <=(Money left, Money right) => right.IsGreaterThanOrEqual(left);

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Cannot operate on different currencies: {Currency} and {other.Currency}.");
    }

    public override string ToString() => $"{Amount:N0} {Currency}";
}