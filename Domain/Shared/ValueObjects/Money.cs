namespace Domain.Shared.ValueObjects;

public sealed class Money : ValueObject, IComparable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    private const string DefaultCurrency = "IRR";

    private Money(decimal amount, string currency = DefaultCurrency)
    {
        Amount = amount;
        Currency = currency;
    }

    #region Factory Methods

    public static Money Zero(string currency = DefaultCurrency)
    {
        return new Money(0, currency);
    }

    public static Money FromDecimal(decimal amount, string currency = DefaultCurrency)
    {
        return new Money(Math.Round(amount, 0), currency);
    }

    public static Money FromToman(decimal tomanAmount)
    {
        return new Money(tomanAmount * 10, DefaultCurrency);
    }

    public sealed class MoneyToDecimalConverter : IValueConverter<Money?, decimal>
    {
        public decimal Convert(Money? sourceMember, ResolutionContext context)
            => sourceMember?.Amount ?? 0m;
    }

    public sealed class DecimalToMoneyConverter : IValueConverter<decimal, Money>
    {
        public Money Convert(decimal sourceMember, ResolutionContext context)
            => FromDecimal(sourceMember);
    }

    #endregion Factory Methods

    #region Arithmetic Operations

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        var result = Amount - other.Amount;

        if (result < 0)
            throw new DomainException("نتیجه عملیات نمی‌تواند منفی باشد.");

        return new Money(result, Currency);
    }

    public Money SubtractAllowNegative(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new DomainException("ضریب نمی‌تواند منفی باشد.");

        return new Money(Math.Round(Amount * factor, 0), Currency);
    }

    public Money Multiply(int quantity)
    {
        if (quantity < 0)
            throw new DomainException("تعداد نمی‌تواند منفی باشد.");

        return new Money(Amount * quantity, Currency);
    }

    public Money Divide(decimal divisor)
    {
        if (divisor == 0)
            throw new DomainException("تقسیم بر صفر امکان‌پذیر نیست.");

        return new Money(Math.Round(Amount / divisor, 0), Currency);
    }

    public Money Percentage(decimal percentage)
    {
        if (percentage < 0 || percentage > 100)
            throw new DomainException("درصد باید بین ۰ تا ۱۰۰ باشد.");

        return new Money(Math.Round(Amount * percentage / 100, 0), Currency);
    }

    public Money Abs()
    {
        return new Money(Math.Abs(Amount), Currency);
    }

    #endregion Arithmetic Operations

    #region Comparison

    public bool IsZero() => Amount == 0;

    public bool IsPositive() => Amount > 0;

    public bool IsNegative() => Amount < 0;

    public bool IsGreaterThan(Money other)
    {
        EnsureSameCurrency(other);
        return Amount > other.Amount;
    }

    public bool IsLessThan(Money other)
    {
        EnsureSameCurrency(other);
        return Amount < other.Amount;
    }

    public bool IsGreaterThanOrEqual(Money other)
    {
        EnsureSameCurrency(other);
        return Amount >= other.Amount;
    }

    public bool IsLessThanOrEqual(Money other)
    {
        EnsureSameCurrency(other);
        return Amount <= other.Amount;
    }

    public int CompareTo(Money? other)
    {
        if (other == null) return 1;
        EnsureSameCurrency(other);
        return Amount.CompareTo(other.Amount);
    }

    #endregion Comparison

    #region Conversion

    public decimal ToToman()
    {
        return Amount / 10;
    }

    public string ToFormattedString()
    {
        return $"{Amount:N0} ریال";
    }

    public string ToTomanString()
    {
        return $"{ToToman():N0} تومان";
    }

    #endregion Conversion

    #region Operators

    public static Money operator +(Money left, Money right)
    {
        return left.Add(right);
    }

    public static Money operator -(Money left, Money right)
    {
        return left.SubtractAllowNegative(right);
    }

    public static Money operator *(Money money, decimal factor)
    {
        return money.Multiply(factor);
    }

    public static Money operator *(Money money, int quantity)
    {
        return money.Multiply(quantity);
    }

    public static Money operator /(Money money, decimal divisor)
    {
        return money.Divide(divisor);
    }

    public static bool operator >(Money left, Money right)
    {
        return left.IsGreaterThan(right);
    }

    public static bool operator <(Money left, Money right)
    {
        return left.IsLessThan(right);
    }

    public static bool operator >=(Money left, Money right)
    {
        return left.IsGreaterThanOrEqual(right);
    }

    public static bool operator <=(Money left, Money right)
    {
        return left.IsLessThanOrEqual(right);
    }

    #endregion Operators

    #region Private Methods

    private void EnsureSameCurrency(Money other)
    {
        if (other == null)
            throw new DomainException("مقدار پول نمی‌تواند null باشد.");

        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new DomainException($"امکان انجام عملیات با واحدهای پولی متفاوت ({Currency}, {other.Currency}) وجود ندارد.");
    }

    #endregion Private Methods

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency.ToUpperInvariant();
    }

    public override string ToString() => ToTomanString();

    public static implicit operator decimal(Money money) => money.Amount;
}