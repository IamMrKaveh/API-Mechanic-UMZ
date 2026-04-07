namespace Domain.Security.ValueObjects;

public sealed record UserSessionId
{
    public Guid Value { get; }

    private UserSessionId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("UserSessionId cannot be empty.", nameof(value));

        Value = value;
    }

    public static UserSessionId NewId() => new(Guid.NewGuid());

    public static UserSessionId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}