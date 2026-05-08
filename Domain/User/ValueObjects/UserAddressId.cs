namespace Domain.User.ValueObjects;

public sealed record UserAddressId : IStronglyTypedId
{
    public Guid Value { get; }

    private UserAddressId(Guid value) => Value = value;

    public static UserAddressId NewId() => new(Guid.NewGuid());

    public static UserAddressId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("UserAddressId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(UserAddressId id) => id.Value;
}