namespace Domain.Cart.ValueObjects;

public sealed record CartItemId(Guid Value)
{
    public static CartItemId NewId() => new(Guid.NewGuid());
    public static CartItemId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}