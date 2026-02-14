namespace Domain.Product.ValueObjects;

public sealed class Price : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Price(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Price Create(decimal amount, string currency = "IRR")
    {
        if (amount < 0)
            throw new DomainException("قیمت نمی‌تواند منفی باشد.");

        return new Price(amount, currency);
    }

    public static Price Zero(string currency = "IRR") => new(0, currency);

    public Price Add(Price other)
    {
        EnsureSameCurrency(other);
        return new Price(Amount + other.Amount, Currency);
    }

    public Price Subtract(Price other)
    {
        EnsureSameCurrency(other);
        var result = Amount - other.Amount;
        return new Price(Math.Max(0, result), Currency);
    }

    public Price Multiply(int quantity)
    {
        if (quantity < 0)
            throw new DomainException("تعداد نمی‌تواند منفی باشد.");

        return new Price(Amount * quantity, Currency);
    }

    public decimal CalculateDiscountPercentage(Price originalPrice)
    {
        if (originalPrice.Amount <= 0 || Amount >= originalPrice.Amount)
            return 0;

        return Math.Round((1 - (Amount / originalPrice.Amount)) * 100, 2);
    }

    private void EnsureSameCurrency(Price other)
    {
        if (Currency != other.Currency)
            throw new DomainException("ارزهای متفاوت قابل محاسبه نیستند.");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:N0} {Currency}";
}