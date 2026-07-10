using System.ComponentModel.DataAnnotations;

namespace Application.Wallet.Options;

public sealed class WalletTransferOptions
{
    public const string SectionName = "WalletTransfer";

    [Range(typeof(decimal), "1", "79228162514264337593543950335")]
    public decimal MinimumAmount { get; init; } = 10000;

    [Range(typeof(decimal), "1", "79228162514264337593543950335")]
    public decimal MaximumAmount { get; init; } = 1000000000;

    [Range(typeof(decimal), "1", "79228162514264337593543950335")]
    public decimal DailyLimit { get; init; } = 50000000;

    [Range(4, 10)]
    public int OtpLength { get; init; } = 6;

    [Range(30, 3600)]
    public int OtpTtlSeconds { get; init; } = 180;

    [Range(1, 1000)]
    public int MaxPendingTransfersPerHour { get; init; } = 5;

    [Required(AllowEmptyStrings = false)]
    public string Currency { get; init; } = "IRT";
}