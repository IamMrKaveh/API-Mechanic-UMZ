namespace Domain.Wishlist.Exceptions;

public sealed class WishlistItemAlreadyExistsException(UserId userId, ProductId productId) : DomainException($"محصول '{productId}' قبلاً در لیست علاقه‌مندی‌های کاربر '{userId}' وجود دارد.")
{
    public UserId UserId { get; } = userId;
    public ProductId ProductId { get; } = productId;
}