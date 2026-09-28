using Domain.Wallet.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Localization;

namespace Domain.Wallet.Exceptions;

public sealed class WalletInactiveException(WalletId walletId)
    : ConflictException<WalletId>(
        DomainErrorCodes.Wallet.Inactive,
        walletId,
        $"Wallet '{walletId}' is inactive and cannot process transactions.",
        new Dictionary<string, object?>
        {
            ["walletId"] = walletId.Value
        })
{
    public WalletId WalletId => Value;
}
