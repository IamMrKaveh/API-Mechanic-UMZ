using SharedKernel.Localization;

namespace Domain.Wallet.Exceptions;

public sealed class InvalidWalletDebitRequestStatusException(string currentStatus) : DomainException(
        DomainErrorCodes.Wallet.DebitRequestInvalidStatus,
        $"Current request status '{currentStatus}' does not allow this operation.",
        new Dictionary<string, object?>
{
["currentStatus"] = currentStatus
})
{
}
