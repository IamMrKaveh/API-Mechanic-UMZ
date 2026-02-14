namespace Domain.Cart.Services;

/// <summary>
/// Domain Service برای عملیات‌هایی که بین چند Aggregate هستند.
/// Stateless - بدون وابستگی به Infrastructure.
/// منطق موجودی و قیمت به Application Layer منتقل شده.
/// </summary>
public class CartDomainService
{
    /// <summary>
    /// تعیین استراتژی ادغام بر اساس قوانین کسب و کار
    /// </summary>
    public CartMergeStrategy DetermineMergeStrategy(Cart userCart, Cart guestCart)
    {
        Guard.Against.Null(userCart, nameof(userCart));
        Guard.Against.Null(guestCart, nameof(guestCart));

        // اگر سبد کاربر خالی است، سبد مهمان را نگه دار
        if (userCart.IsEmpty && !guestCart.IsEmpty)
            return CartMergeStrategy.KeepGuestCart;

        // اگر سبد مهمان خالی است، سبد کاربر را نگه دار
        if (!userCart.IsEmpty && guestCart.IsEmpty)
            return CartMergeStrategy.KeepUserCart;

        // در غیر این صورت، تعداد بیشتر را نگه دار
        return CartMergeStrategy.KeepHigherQuantity;
    }
}