using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Wishlist.Services;

public sealed class WishlistDomainService
{
    private const int MaxWishlistItemsPerUser = 200;

    public static Result ValidateAddItem(
        bool alreadyExists,
        int currentItemCount)
    {
        if (alreadyExists)
            return Result.Failure("این محصول قبلاً در لیست علاقه‌مندی‌های شما وجود دارد.");

        if (currentItemCount >= MaxWishlistItemsPerUser)
            return Result.Failure($"حداکثر تعداد مجاز آیتم در لیست علاقه‌مندی {MaxWishlistItemsPerUser} عدد است.");

        return Result.Success();
    }

    public static Result ValidateRemoveItem(bool exists)
    {
        if (!exists)
            return Result.Failure("آیتم مورد نظر در لیست علاقه‌مندی‌های شما یافت نشد.");

        return Result.Success();
    }

    public static Aggregates.Wishlist CreateItem(UserId userId, ProductId productId)
    {
        Guard.Against.Null(userId, nameof(userId));
        Guard.Against.Null(productId, nameof(productId));

        return Aggregates.Wishlist.Create(userId, productId);
    }
}