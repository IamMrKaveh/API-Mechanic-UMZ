using SharedKernel.ValueObjects;

namespace Infrastructure.Common.Converters;

public sealed class MoneyConverter : ValueConverter<Money, decimal>
{
    public MoneyConverter()
        : base(
            money => money.Amount,
            value => Money.Create(value, "IRT"))
    {
    }
}