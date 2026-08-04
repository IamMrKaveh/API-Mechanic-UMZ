using SharedKernel.Localization;

namespace Domain.Wallet.Exceptions;

public sealed class UnauthorizedWalletDebitApprovalException : DomainException
{
    public UnauthorizedWalletDebitApprovalException()
        : base(
            DomainErrorCodes.Wallet.DebitApprovalUnauthorized,
            "Only the wallet owner can approve or reject this request.")
    {
    }
}
