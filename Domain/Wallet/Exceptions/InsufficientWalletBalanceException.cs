using Domain.Wallet.ValueObjects;
using SharedKernel.Localization;

namespace Domain.Wallet.Exceptions;

public sealed class InsufficientWalletBalanceException(WalletId walletId, Money requested, Money available) : DomainException(
        DomainErrorCodes.Wallet.InsufficientBalance,
        $"Wallet '{walletId}' has insufficient balance. Requested: {requested.Amount:N0} {requested.Currency}, Available: {available.Amount:N0} {available.Currency}.",
        new Dictionary<string, object?>
{
["walletId"] = walletId.Value,
["requestedAmount"] = requested.Amount,
["requestedCurrency"] = requested.Currency,
["availableAmount"] = available.Amount,
["availableCurrency"] = available.Currency
})
{
}
