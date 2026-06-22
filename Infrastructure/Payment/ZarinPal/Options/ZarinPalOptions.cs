using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Payment.ZarinPal.Options;

public sealed class ZarinPalOptions
{
    public const string SectionName = "Zarinpal";

    [Required(AllowEmptyStrings = false)]
    [MinLength(8, ErrorMessage = "MerchantId معتبر نیست.")]
    public string MerchantId { get; init; } = string.Empty;

    public bool IsSandbox { get; init; } = true;

    public string CallbackUrl { get; init; } = "/payment/callback";

    public string ProductionApiBaseUrl { get; init; } = "https://api.zarinpal.com/pg/v4/payment/";
    public string ProductionStartPayBaseUrl { get; init; } = "https://www.zarinpal.com/pg/StartPay/";

    public string SandboxApiBaseUrl { get; init; } = "https://sandbox.zarinpal.com/pg/v4/payment/";
    public string SandboxStartPayBaseUrl { get; init; } = "https://sandbox.zarinpal.com/pg/StartPay/";
}