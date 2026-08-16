using Domain.Wallet.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class IbanNumberBuilder { private string _value = "IR580540105180021273113007";

public IbanNumberBuilder WithValue(string value)
{
    _value = value;
    return this;
}

public IbanNumber Build() => IbanNumber.Create(_value);
}