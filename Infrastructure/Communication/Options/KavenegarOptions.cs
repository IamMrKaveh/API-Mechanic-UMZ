using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Communication.Options;

public sealed class KavenegarOptions
{
    public const string SectionName = "Kavenegar";

    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Sender { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string OtpTemplate { get; init; } = "verify";
}