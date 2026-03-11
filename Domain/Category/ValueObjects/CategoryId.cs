namespace Domain.Category.ValueObjects;

public sealed record CategoryId(Guid Value)
{
    public static CategoryId NewId() => new(Guid.NewGuid());
    public static CategoryId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}