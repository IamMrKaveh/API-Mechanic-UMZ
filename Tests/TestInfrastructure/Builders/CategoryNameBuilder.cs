using Domain.Category.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class CategoryNameBuilder
{
    private static readonly Faker Faker = new();

    private string _value = Faker.Commerce.Categories(1)[0];

    public CategoryNameBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public CategoryName Build() => CategoryName.Create(_value);
}
