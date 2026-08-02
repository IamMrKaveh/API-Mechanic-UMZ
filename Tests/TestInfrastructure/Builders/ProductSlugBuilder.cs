using Domain.Product.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class ProductSlugBuilder
{
    private static readonly Faker Faker = new();

    private string _value = Faker.Random.AlphaNumeric(12).ToLowerInvariant();

    public ProductSlugBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public ProductSlug Build() => ProductSlug.Create(_value);
}
