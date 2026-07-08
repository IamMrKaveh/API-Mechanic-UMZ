namespace Application.Wallet.Options;

public sealed class WalletTransferOptions
{
    public const string SectionName = "WalletTransfer";

    public decimal MinimumAmount { get; set; } = 10_000m;
    public decimal MaximumAmount { get; set; } = 1_000_000_000m;
    public decimal DailyLimit { get; set; } = 50_000_000m;
    public int OtpLength { get; set; } = 6;
    public int OtpTtlSeconds { get; set; } = 180;
    public int MaxPendingTransfersPerHour { get; set; } = 10;
    public string Currency { get; set; } = "IRT";
}