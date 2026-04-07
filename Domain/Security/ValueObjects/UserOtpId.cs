using System;

namespace Domain.Security.ValueObjects;

public sealed record UserOtpId
{
    public Guid Value { get; }

    private UserOtpId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("UserOtpId cannot be empty.", nameof(value));

        Value = value;
    }

    public static UserOtpId NewId() => new(Guid.NewGuid());

    public static UserOtpId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}