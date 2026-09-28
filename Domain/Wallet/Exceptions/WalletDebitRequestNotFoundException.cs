using Domain.Wallet.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Localization;

namespace Domain.Wallet.Exceptions;

public sealed class WalletDebitRequestNotFoundException(WalletDebitRequestId requestId)
    : NotFoundException<WalletDebitRequestId>(
        DomainErrorCodes.Wallet.DebitRequestNotFound,
        requestId,
        $"Debit request with id {requestId.Value} was not found.",
        new Dictionary<string, object?>
        {
            ["requestId"] = requestId.Value
        })
{
    public WalletDebitRequestId RequestId => Value;
}
