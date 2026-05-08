namespace Domain.Media.ValueObjects;

public sealed record MediaId : IStronglyTypedId
{
    public Guid Value { get; }

    private MediaId(Guid value) => Value = value;

    public static MediaId NewId() => new(Guid.NewGuid());

    public static MediaId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("MediaId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(MediaId id) => id.Value;
}