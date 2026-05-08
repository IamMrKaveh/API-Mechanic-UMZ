namespace Domain.Security.ValueObjects;

public sealed record SessionId : IStronglyTypedId
{
    public Guid Value { get; }

    private SessionId(Guid value) => Value = value;

    public static SessionId NewId() => new(Guid.NewGuid());

    public static SessionId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("SessionId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(SessionId id) => id.Value;
}