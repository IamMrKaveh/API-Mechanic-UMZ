using Domain.Brand.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class BrandSlugBuilder
{
    private static readonly Faker Faker = new();

    private string _value = Faker.Random.AlphaNumeric(12).ToLowerInvariant();

    public BrandSlugBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public BrandSlug Build() => BrandSlug.Create(_value);
}
