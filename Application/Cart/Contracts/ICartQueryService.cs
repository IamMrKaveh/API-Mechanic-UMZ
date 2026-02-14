namespace Application.Cart.Contracts;

/// <summary>
/// Query Service برای خواندن بهینه داده‌های سبد خرید.
/// مستقیماً DTO برمی‌گرداند - بدون بارگذاری Aggregate.
/// </summary>
public interface ICartQueryService
{
    /// <summary>
    /// دریافت سبد خرید کامل با اطلاعات محصولات و تصاویر
    /// </summary>
    Task<CartDetailDto?> GetCartDetailAsync(int? userId, string? guestToken, CancellationToken ct = default);

    /// <summary>
    /// خلاصه سبد برای نمایش در هدر سایت
    /// </summary>
    Task<CartSummaryDto> GetCartSummaryAsync(int? userId, string? guestToken, CancellationToken ct = default);

    /// <summary>
    /// بررسی آمادگی سبد برای پرداخت - شامل بررسی موجودی و قیمت
    /// </summary>
    Task<CartCheckoutValidationDto> ValidateCartForCheckoutAsync(int? userId, string? guestToken, CancellationToken ct = default);
}