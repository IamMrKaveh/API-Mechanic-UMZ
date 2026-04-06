namespace Domain.Cart.ValueObjects;

public sealed record CartId(Guid Value)
{
    public static CartId NewId() => new(Guid.NewGuid());
    public static CartId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}