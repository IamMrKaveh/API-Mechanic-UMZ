namespace Infrastructure.Payment.ZarinPal.Options;

public sealed class ZarinPalOptions
{
    public const string SectionName = "Zarinpal";

    [Required(AllowEmptyStrings = false)]
    public string MerchantId { get; set; } = "00000000-0000-0000-0000-000000000000";

    public string? SandboxMerchantId { get; set; } = "00000000-0000-0000-0000-000000000000";

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string ApiBaseUrl { get; set; } = "https://api.zarinpal.com/pg/v4/payment/";

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string SandboxApiBaseUrl { get; set; } = "https://sandbox.zarinpal.com/pg/v4/payment/";

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string StartPayBaseUrl { get; set; } = "https://www.zarinpal.com/pg/StartPay/";

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string SandboxStartPayBaseUrl { get; set; } = "https://sandbox.zarinpal.com/pg/StartPay/";

    public bool UseSandbox { get; set; } = true;

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}