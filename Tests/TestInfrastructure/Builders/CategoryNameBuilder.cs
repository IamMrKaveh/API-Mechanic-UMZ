using Domain.Category.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class CategoryNameBuilder
{
    private string _value = $"Cat-{Guid.NewGuid():N}"[..20];

    public CategoryNameBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public CategoryName Build() => CategoryName.Create(_value);
}
