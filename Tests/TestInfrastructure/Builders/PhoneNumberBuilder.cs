using Domain.User.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class PhoneNumberBuilder
{
    private static readonly Faker Faker = new();

    private string _value = "09" + Faker.Random.String2(9, "0123456789");

    public PhoneNumberBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public PhoneNumber Build() => PhoneNumber.Create(_value);
}
