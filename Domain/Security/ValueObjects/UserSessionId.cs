namespace Domain.Security.ValueObjects;

public sealed record UserSessionId(Guid Value)
{
    public static UserSessionId NewId() => new(Guid.NewGuid());
    public static UserSessionId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}