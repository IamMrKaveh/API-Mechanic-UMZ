namespace Application.Cache.Contracts;

/// <summary>
/// سرویس Invalidation کش مبتنی بر رویداد.
/// به جای پاک کردن همه کش، هدفمند عمل می‌کند.
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>پاک کردن کش محصول و آیتم‌های وابسته</summary>
    Task InvalidateProductAsync(int productId, CancellationToken ct = default);

    /// <summary>پاک کردن کش دسته‌بندی</summary>
    Task InvalidateCategoryAsync(int categoryId, CancellationToken ct = default);

    /// <summary>پاک کردن کش موجودی یک Variant</summary>
    Task InvalidateInventoryAsync(int variantId, CancellationToken ct = default);

    /// <summary>پاک کردن کش سبد خرید کاربر</summary>
    Task InvalidateCartAsync(string cartKey, CancellationToken ct = default);

    /// <summary>پاک کردن کش سفارش‌های کاربر</summary>
    Task InvalidateUserOrdersAsync(int userId, CancellationToken ct = default);

    /// <summary>پاک کردن کش بر اساس Pattern (Wildcard)</summary>
    Task InvalidateByPatternAsync(string pattern, CancellationToken ct = default);
}