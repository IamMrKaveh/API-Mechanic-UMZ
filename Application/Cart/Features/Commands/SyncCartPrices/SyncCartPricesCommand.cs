namespace Application.Cart.Features.Commands.SyncCartPrices;

/// <summary>
/// همگام‌سازی قیمت‌های سبد خرید با قیمت‌های فعلی محصولات.
/// قبل از نمایش سبد یا Checkout فراخوانی می‌شود.
/// </summary>
public record SyncCartPricesCommand : IRequest<ServiceResult<SyncCartPricesResult>>;