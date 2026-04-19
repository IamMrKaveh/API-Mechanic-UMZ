namespace Infrastructure.Payment.ZarinPal.Options;

public sealed class ZarinPalOptions
{
    public const string SectionName = "Zarinpal";

    [Required(AllowEmptyStrings = false)]
    [MinLength(8, ErrorMessage = "MerchantId معتبر نیست.")]
    public string MerchantId { get; init; } = string.Empty;
}