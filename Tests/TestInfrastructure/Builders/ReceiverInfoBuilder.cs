using Domain.Order.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class ReceiverInfoBuilder
{
    private string _fullName = "علی رضایی";
    private string _phoneNumber = "09121234567";

    public ReceiverInfoBuilder WithFullName(string value)
    {
        _fullName = value;
        return this;
    }

    public ReceiverInfoBuilder WithPhoneNumber(string value)
    {
        _phoneNumber = value;
        return this;
    }

    public ReceiverInfo Build() => ReceiverInfo.Create(_fullName, _phoneNumber);
}
