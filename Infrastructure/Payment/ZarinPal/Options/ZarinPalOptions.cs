namespace Infrastructure.Payment.ZarinPal.Options;

public sealed class ZarinPalOptions
{
    public const string SectionName = "Payment:ZarinPal";

    public string MerchantId { get; set; } = string.Empty;

    public string? SandboxMerchantId { get; set; }

    public string ApiBaseUrl { get; set; } = "https://payment.zarinpal.com/";

    public string? SandboxApiBaseUrl { get; set; } = "https://sandbox.zarinpal.com/";

    public string StartPayBaseUrl { get; set; } = "https://payment.zarinpal.com/pg/StartPay";

    public string? SandboxStartPayBaseUrl { get; set; } = "https://sandbox.zarinpal.com/pg/StartPay";

    public bool UseSandbox { get; set; } = true;

    public int TimeoutSeconds { get; set; } = 30;
}