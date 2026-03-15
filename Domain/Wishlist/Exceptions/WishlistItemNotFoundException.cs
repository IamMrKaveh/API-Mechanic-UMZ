using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.ValueObjects;

namespace Domain.Wishlist.Exceptions;

public sealed class WishlistItemNotFoundException : DomainException
{
    public WishlistItemNotFoundException(WishlistId wishlistId)
        : base($"آیتم علاقه‌مندی با شناسه '{wishlistId}' یافت نشد.")
    {
        WishlistId = wishlistId;
    }

    public WishlistItemNotFoundException(UserId userId, ProductId productId)
        : base($"محصول '{productId}' در لیست علاقه‌مندی‌های کاربر '{userId}' یافت نشد.")
    {
    }

    public WishlistId? WishlistId { get; }
}