namespace Domain.Security.ValueObjects;

public sealed record UserOtpId(Guid Value)
{
    public static UserOtpId NewId() => new(Guid.NewGuid());
    public static UserOtpId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}