namespace Domain.User.ValueObjects;

public sealed record UserId : IStronglyTypedId
{
    public Guid Value { get; }

    private UserId(Guid value) => Value = value;

    public static UserId NewId() => new(Guid.NewGuid());

    public static UserId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("UserId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(UserId id) => id.Value;
}