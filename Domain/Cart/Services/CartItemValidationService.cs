namespace Domain.Cart.Services;

/// <summary>
/// سرویس دامنه برای اعتبارسنجی موجودی در Domain Layer
/// این سرویس قوانین تجاری بررسی موجودی را در Domain نگه می‌دارد
/// </summary>
public class CartItemValidationService
{
    /// <summary>
    /// بررسی اینکه آیا می‌توان یک آیتم با تعداد مشخص به سبد اضافه کرد
    /// </summary>
    public (bool IsValid, string? Error) ValidateAddToCart(
        int requestedQuantity,
        int availableStock,
        bool isUnlimited,
        int currentCartQuantity = 0)
    {
        if (!isUnlimited)
        {
            var totalRequested = currentCartQuantity + requestedQuantity;
            if (availableStock < requestedQuantity)
                return (false, $"موجودی کافی نیست. موجودی قابل دسترس: {availableStock}");

            if (totalRequested > availableStock)
                return (false, $"موجودی کافی نیست. موجودی قابل دسترس: {availableStock}، تعداد در سبد: {currentCartQuantity}");
        }

        return (true, null);
    }

    /// <summary>
    /// بررسی اینکه آیا می‌توان تعداد آیتم در سبد را به مقدار مشخص تغییر داد
    /// </summary>
    public (bool IsValid, string? Error) ValidateUpdateQuantity(
        int newQuantity,
        int availableStock,
        bool isUnlimited)
    {
        if (!isUnlimited && availableStock < newQuantity)
            return (false, $"موجودی کافی نیست. موجودی قابل دسترس: {availableStock}");

        return (true, null);
    }
}