using Domain.User.Aggregates;
using Domain.User.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class UserBuilder
{
    private FullName _fullName = new FullNameBuilder().Build();
    private Email _email = Email.Create($"user{Guid.NewGuid():N}@example.com");
    private string _passwordHash = "hashed-password-value";
    private PhoneNumber? _phoneNumber;

    public UserBuilder WithFullName(FullName fullName)
    {
        _fullName = fullName;
        return this;
    }

    public UserBuilder WithEmail(Email email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithEmail(string value)
    {
        _email = Email.Create(value);
        return this;
    }

    public UserBuilder WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    public UserBuilder WithPhoneNumber(PhoneNumber? phoneNumber)
    {
        _phoneNumber = phoneNumber;
        return this;
    }

    public User Build() =>
        User.Create(_fullName, _email, _passwordHash, _phoneNumber);
}
