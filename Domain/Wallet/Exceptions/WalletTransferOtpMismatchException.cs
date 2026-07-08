namespace Domain.Wallet.Exceptions;

public sealed class WalletTransferOtpMismatchException(int remainingAttempts) : DomainException(remainingAttempts > 0
            ? $"کد تأیید نادرست است. {remainingAttempts} تلاش دیگر باقی مانده است."
            : "تعداد تلاش‌های مجاز به پایان رسیده است.")
{
    public override string ErrorCode => "WALLET_TRANSFER_OTP_MISMATCH";

    public int RemainingAttempts { get; } = remainingAttempts;
}