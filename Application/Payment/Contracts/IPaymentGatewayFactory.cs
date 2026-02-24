namespace Application.Payment.Contracts;

/// <summary>
/// Factory برای انتخاب درگاه پرداخت مناسب.
/// امکان افزودن درگاه‌های جدید بدون تغییر کد موجود را فراهم می‌کند (OCP).
/// </summary>
public interface IPaymentGatewayFactory
{
    /// <summary>
    /// دریافت درگاه پرداخت بر اساس نام.
    /// </summary>
    IPaymentGateway GetGateway(string gatewayName);

    /// <summary>
    /// دریافت درگاه پرداخت پیش‌فرض (از تنظیمات).
    /// </summary>
    IPaymentGateway GetDefaultGateway();

    /// <summary>
    /// لیست درگاه‌های موجود.
    /// </summary>
    IEnumerable<string> GetAvailableGateways();
}



public sealed class PaymentGatewayOptions
{
    public const string SectionName = "PaymentGateway";

    /// <summary>نام درگاه پیش‌فرض (ZarinPal, Mellat, Saman, ...)</summary>
    public string DefaultGateway { get; set; } = "ZarinPal";

    /// <summary>فعال‌سازی Mock Gateway در محیط توسعه</summary>
    public bool EnableMockGateway { get; set; } = false;
}