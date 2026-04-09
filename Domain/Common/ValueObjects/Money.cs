namespace Domain.Common.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "IRT")
    {
        if (amount < 0)
            throw new ArgumentException("Money amount cannot be negative.", nameof(amount));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty.", nameof(currency));
        return new Money(amount, currency.ToUpperInvariant().Trim());
    }

    public static Money FromDecimal(decimal amount, string currency = "IRT") => Create(amount, currency);

    public static Money Zero(string currency = "IRT") => new(0, currency);

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

    public bool IsLessThan(Money other)
    {
        EnsureSameCurrency(other);
        return Amount < other.Amount;
    }

    public bool IsLessThanOrEqual(Money other)
    {
        EnsureSameCurrency(other);
        return Amount <= other.Amount;
    }

    public bool IsZero() => Amount == 0;

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cannot operate on money with different currencies: {Currency} and {other.Currency}");
    }

    public string ToTomanString()
    {
        var tomanAmount = Currency == "IRR" ? Amount / 10 : Amount;
        return $"{tomanAmount:N0} تومان";
    }

    public decimal ToTomanDecimal()
    {
        var tomanAmount = Currency == "IRR" ? Amount / 10 : Amount;
        return tomanAmount;
    }

    public string ToRialString()
    {
        var rialAmount = Currency == "IRT" ? Amount * 10 : Amount;
        return $"{rialAmount:N0} ریال";
    }

    public decimal ToRialDecimal()
    {
        var rialAmount = Currency == "IRT" ? Amount * 10 : Amount;
        return rialAmount;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}