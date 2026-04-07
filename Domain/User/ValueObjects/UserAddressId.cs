using System;

namespace Domain.User.ValueObjects;

public sealed record UserAddressId
{
    public Guid Value { get; }

    private UserAddressId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("UserAddressId cannot be empty.", nameof(value));

        Value = value;
    }

    public static UserAddressId NewId() => new(Guid.NewGuid());

    public static UserAddressId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}