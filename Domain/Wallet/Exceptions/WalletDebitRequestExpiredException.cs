using SharedKernel.Localization;

namespace Domain.Wallet.Exceptions;

public sealed class WalletDebitRequestExpiredException : DomainException
{
    public WalletDebitRequestExpiredException()
        : base(
            DomainErrorCodes.Wallet.DebitRequestExpired,
            "The response window for this request has expired.")
    {
    }
}
