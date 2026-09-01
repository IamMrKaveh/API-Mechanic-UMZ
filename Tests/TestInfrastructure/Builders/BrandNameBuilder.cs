using Domain.Brand.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class BrandNameBuilder
{
    private string _value = $"Brand-{Guid.NewGuid():N}"[..20];

    public BrandNameBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public BrandName Build() => BrandName.Create(_value);
}
