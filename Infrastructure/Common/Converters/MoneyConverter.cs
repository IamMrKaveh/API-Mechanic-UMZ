namespace Infrastructure.Common.Converters;

public sealed class MoneyConverter : ValueConverter<Money, decimal>
{
    public MoneyConverter()
        : base(
            money => money.Amount,
            amount => FromDecimal(amount))
    {
    }

    private static Money FromDecimal(decimal amount)
    {
        if (amount < 0m)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        return Money.Create(amount, "IRT");
    }
}
