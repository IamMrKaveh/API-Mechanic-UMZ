using SharedKernel.Localization;

namespace Domain.Wallet.Exceptions;

public sealed class WalletTransferOtpMismatchException(int remainingAttempts) : DomainException(
        remainingAttempts > 0
                ? DomainErrorCodes.Wallet.TransferOtpMismatch
                : DomainErrorCodes.Wallet.TransferOtpAttemptsUsed,
        remainingAttempts > 0
                ? $"OTP code is incorrect. {remainingAttempts} attempts remaining."
                : "Maximum OTP attempts exceeded.",
        new Dictionary<string, object?>
{
["remainingAttempts"] = remainingAttempts
})
{
}
