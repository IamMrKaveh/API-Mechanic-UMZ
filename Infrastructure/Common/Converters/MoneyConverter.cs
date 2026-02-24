namespace Infrastructure.Common.Converters;

public class MoneyConverter : ValueConverter<Money, decimal>
{
    public MoneyConverter()
        : base(
            money => money.Amount,
            amount => Money.FromDecimal(amount),
            new ConverterMappingHints(precision: 18, scale: 2))
    {
    }
}