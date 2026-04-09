namespace Domain.Security.ValueObjects;

public sealed record SessionId
{
    public Guid Value { get; }

    private SessionId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("SessionId cannot be empty.", nameof(value));

        Value = value;
    }

    public static SessionId NewId() => new(Guid.NewGuid());

    public static SessionId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}