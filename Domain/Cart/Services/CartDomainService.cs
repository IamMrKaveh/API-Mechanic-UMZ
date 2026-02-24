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

        
        if (userCart.IsEmpty && !guestCart.IsEmpty)
            return CartMergeStrategy.KeepGuestCart;

        
        if (!userCart.IsEmpty && guestCart.IsEmpty)
            return CartMergeStrategy.KeepUserCart;

        
        return CartMergeStrategy.KeepHigherQuantity;
    }
}