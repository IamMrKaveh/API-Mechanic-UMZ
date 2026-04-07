namespace Domain.Wishlist.ValueObjects;

public sealed record WishlistId
{
    public Guid Value { get; }

    private WishlistId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("WishlistId cannot be empty.", nameof(value));

        Value = value;
    }

    public static WishlistId NewId() => new(Guid.NewGuid());

    public static WishlistId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}