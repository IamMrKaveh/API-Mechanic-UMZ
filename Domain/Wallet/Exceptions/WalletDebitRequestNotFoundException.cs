using Domain.Wallet.ValueObjects;
using SharedKernel.Localization;

namespace Domain.Wallet.Exceptions;

public sealed class WalletDebitRequestNotFoundException(WalletDebitRequestId requestId) : DomainException(
        DomainErrorCodes.Wallet.DebitRequestNotFound,
        $"Debit request with id {requestId.Value} was not found.",
        new Dictionary<string, object?>
{
["requestId"] = requestId.Value
})
{
}
