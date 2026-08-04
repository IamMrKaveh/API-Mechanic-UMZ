using SharedKernel.Localization;

namespace Domain.Wallet.Exceptions;

public sealed class InvalidWalletAmountException(decimal amount) : DomainException(
        DomainErrorCodes.Wallet.InvalidAmount,
        $"Wallet transaction amount '{amount}' is invalid. Amount must be greater than zero.",
        new Dictionary<string, object?>
{
["amount"] = amount
})
{
}
