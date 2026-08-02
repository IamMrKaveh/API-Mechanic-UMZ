using Domain.Variant.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class SkuBuilder
{
    private static readonly Faker Faker = new();

    private string _value = Faker.Random.AlphaNumeric(10).ToUpperInvariant();

    public SkuBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public Sku Build() => Sku.Create(_value);
}
