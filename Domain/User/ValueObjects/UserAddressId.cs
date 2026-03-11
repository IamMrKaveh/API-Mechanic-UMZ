namespace Domain.User.ValueObjects;

public sealed record UserAddressId(Guid Value)
{
    public static UserAddressId NewId() => new(Guid.NewGuid());
    public static UserAddressId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}