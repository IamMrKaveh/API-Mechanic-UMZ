using Domain.User.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class FullNameBuilder
{
    private static readonly Faker Faker = new();

    private string? _firstName = Faker.Random.String2(3, 15, "abcdefghijklmnopqrstuvwxyz");
    private string? _lastName = Faker.Random.String2(3, 15, "abcdefghijklmnopqrstuvwxyz");

    public FullNameBuilder WithFirstName(string? value)
    {
        _firstName = value;
        return this;
    }

    public FullNameBuilder WithLastName(string? value)
    {
        _lastName = value;
        return this;
    }

    public FullName Build() => FullName.Create(_firstName, _lastName);
}
