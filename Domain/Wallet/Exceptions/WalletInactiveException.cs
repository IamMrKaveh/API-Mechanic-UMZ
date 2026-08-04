using Domain.Wallet.ValueObjects;
using SharedKernel.Localization;

namespace Domain.Wallet.Exceptions;

public sealed class WalletInactiveException(WalletId walletId) : DomainException(
        DomainErrorCodes.Wallet.Inactive,
        $"Wallet '{walletId}' is inactive and cannot process transactions.",
        new Dictionary<string, object?>
{
["walletId"] = walletId.Value
})
{
}
