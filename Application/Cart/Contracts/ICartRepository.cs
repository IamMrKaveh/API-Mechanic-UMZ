namespace Application.Cart.Contracts;

/// <summary>
/// Repository Interface برای Cart Aggregate.
/// فقط عملیات‌های ضروری برای Persistence.
/// </summary>
public interface ICartRepository
{
    Task<Domain.Cart.Cart?> GetByUserIdAsync(
        int userId,
        CancellationToken ct = default
        );

    Task<Domain.Cart.Cart?> GetByGuestTokenAsync(
        string guestToken,
        CancellationToken ct = default
        );

    Task<Domain.Cart.Cart?> GetCartAsync(
        int? userId,
        string? guestToken,
        CancellationToken ct = default
        );

    Task AddAsync(
        Domain.Cart.Cart cart,
        CancellationToken ct = default
        );

    void Delete(
        Domain.Cart.Cart cart
        );

    Task<bool> ExistsForUserAsync(
        int userId,
        CancellationToken ct = default
        );

    Task<bool> ExistsForGuestAsync(
        string guestToken,
        CancellationToken ct = default
        );

    Task<int> DeleteExpiredGuestCartsAsync(
        DateTime olderThan,
        CancellationToken ct = default
        );

    Task<int> GetItemsCountAsync(
        int? userId,
        string? guestToken,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت واریانت بر اساس شناسه (برای اعتبارسنجی سبد خرید)
    /// </summary>
    Task<ProductVariant?> GetVariantByIdAsync(
        int variantId,
        CancellationToken ct = default
        );

    /// <summary>
    /// پاک کردن سبد خرید کاربر
    /// </summary>
    Task ClearCartAsync(
        int userId,
        CancellationToken ct = default
        );
}