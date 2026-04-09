using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Wishlist.Exceptions;

public sealed class WishlistItemAlreadyExistsException : DomainException
{
    public UserId UserId { get; }
    public ProductId ProductId { get; }

    public override string ErrorCode => "WISHLIST_ITEM_ALREADY_EXISTS";

    public WishlistItemAlreadyExistsException(UserId userId, ProductId productId)
        : base($"محصول '{productId}' قبلاً در لیست علاقه‌مندی‌های کاربر '{userId}' وجود دارد.")
    {
        UserId = userId;
        ProductId = productId;
    }
}