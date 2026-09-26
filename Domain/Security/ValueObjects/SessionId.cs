namespace Domain.Security.ValueObjects;

public sealed record SessionId : StronglyTypedId<SessionId>
{
    private SessionId(Guid value) : base(value) { }

    public static implicit operator Guid(SessionId id) => id.Value;

    public override string ToString() => Value.ToString();
}
