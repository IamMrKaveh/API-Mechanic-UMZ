namespace Domain.Wishlist.ValueObjects;

public sealed record WishlistId : IStronglyTypedId
{
    public Guid Value { get; }

    private WishlistId(Guid value) => Value = value;

    public static WishlistId NewId() => new(Guid.NewGuid());

    public static WishlistId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("WishlistId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(WishlistId id) => id.Value;
}