using Domain.Brand.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class BrandNameBuilder
{
    private static readonly Faker Faker = new();

    private string _value = Faker.Company.CompanyName();

    public BrandNameBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public BrandName Build() => BrandName.Create(_value);
}
