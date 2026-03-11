namespace Domain.Media.ValueObjects;

public sealed record MediaId(Guid Value)
{
    public static MediaId NewId() => new(Guid.NewGuid());
    public static MediaId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}