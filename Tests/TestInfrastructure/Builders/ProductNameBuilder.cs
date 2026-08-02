using Domain.Product.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class ProductNameBuilder
{
    private static readonly Faker Faker = new();

    private string _value = Faker.Commerce.ProductName();

    public ProductNameBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public ProductName Build() => ProductName.Create(_value);
}
