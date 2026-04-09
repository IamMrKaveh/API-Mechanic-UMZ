namespace Domain.Media.ValueObjects;

public sealed record MediaId
{
    public Guid Value { get; }

    private MediaId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("MediaId cannot be empty.", nameof(value));

        Value = value;
    }

    public static MediaId NewId() => new(Guid.NewGuid());

    public static MediaId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}