namespace Domain.Wishlist.Aggregates;

public sealed class Wishlist : AggregateRoot<WishlistId>, IAuditable
{
    private Wishlist()
    { }

    public UserId UserId { get; private set; } = default!;
    public ProductId ProductId { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Wishlist(WishlistId id, UserId userId, ProductId productId) : base(id)
    {
        UserId = userId;
        ProductId = productId;
        CreatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WishlistItemAddedEvent(id, userId, productId));
    }

    public static Wishlist Create(UserId userId, ProductId productId)
    {
        Guard.Against.Null(userId, nameof(userId));
        Guard.Against.Null(productId, nameof(productId));

        return new Wishlist(WishlistId.NewId(), userId, productId);
    }

    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}