namespace Infrastructure.Payment.ZarinPal.Options;

public sealed class ZarinPalOptions
{
    public const string SectionName = "Zarinpal";

    [Required]
    public string MerchantId { get; set; } = "00000000-0000-0000-0000-000000000000";

    public string? SandboxMerchantId { get; set; } = "00000000-0000-0000-0000-000000000000";

    public string ApiBaseUrl { get; set; } = "https://api.zarinpal.com/pg/v4/payment/";

    public string SandboxApiBaseUrl { get; set; } = "https://sandbox.zarinpal.com/pg/v4/payment/";

    public string StartPayBaseUrl { get; set; } = "https://www.zarinpal.com/pg/StartPay/";

    public string SandboxStartPayBaseUrl { get; set; } = "https://sandbox.zarinpal.com/pg/StartPay/";

    public bool UseSandbox { get; set; } = true;

    public int TimeoutSeconds { get; set; } = 30;
}