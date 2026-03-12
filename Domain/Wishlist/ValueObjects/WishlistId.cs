namespace Domain.Wishlist.ValueObjects;

public sealed record WishlistId(Guid Value)
{
    public static WishlistId NewId() => new(Guid.NewGuid());
    public static WishlistId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}