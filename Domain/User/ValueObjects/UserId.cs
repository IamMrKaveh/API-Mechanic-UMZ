namespace Domain.User.ValueObjects;

public sealed record UserId(Guid Value)
{
    public static UserId NewId() => new(Guid.NewGuid());
    public static UserId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}