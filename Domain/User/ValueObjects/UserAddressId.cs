namespace Domain.User.ValueObjects;

public sealed record UserAddressId : StronglyTypedId<UserAddressId>
{
    private UserAddressId(Guid value) : base(value) { }

    public static implicit operator Guid(UserAddressId id) => id.Value;

    public override string ToString() => Value.ToString();
}
