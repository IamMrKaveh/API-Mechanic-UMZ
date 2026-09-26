namespace Domain.User.ValueObjects;

public sealed record UserId : StronglyTypedId<UserId>
{
    private UserId(Guid value) : base(value) { }

    public static implicit operator Guid(UserId id) => id.Value;

    public override string ToString() => Value.ToString();
}
