namespace Domain.Wallet.Enums;

public enum WalletTransferStatus
{
    PendingOtp = 1,
    Completed = 2,
    Cancelled = 3,
    Expired = 4,
    Failed = 5
}