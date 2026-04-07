namespace Domain.Security.ValueObjects;

public sealed record OtpId
{
    public Guid Value { get; }

    private OtpId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("UserOtpId cannot be empty.", nameof(value));

        Value = value;
    }

    public static OtpId NewId() => new(Guid.NewGuid());

    public static OtpId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}