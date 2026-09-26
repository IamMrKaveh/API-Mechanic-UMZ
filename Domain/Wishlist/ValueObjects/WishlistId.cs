namespace Domain.Wishlist.ValueObjects;

public sealed record WishlistId : StronglyTypedId<WishlistId>
{
    private WishlistId(Guid value) : base(value) { }

    public static implicit operator Guid(WishlistId id) => id.Value;

    public override string ToString() => Value.ToString();
}
