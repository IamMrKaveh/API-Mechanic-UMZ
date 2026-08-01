using Domain.Category.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class CategorySlugBuilder
{
    private static readonly Faker Faker = new();

    private string _value = Faker.Random.AlphaNumeric(12).ToLowerInvariant();

    public CategorySlugBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public CategorySlug Build() => CategorySlug.Create(_value);
}
