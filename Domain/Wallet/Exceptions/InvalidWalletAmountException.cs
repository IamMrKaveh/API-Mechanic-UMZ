using SharedKernel.Exceptions;
using SharedKernel.Localization;

namespace Domain.Wallet.Exceptions;

public sealed class InvalidWalletAmountException(decimal amount)
    : SingleValueDomainException<decimal>(
        DomainErrorCodes.Wallet.InvalidAmount,
        amount,
        $"Wallet transaction amount '{amount}' is invalid. Amount must be greater than zero.",
        new Dictionary<string, object?>
        {
            ["amount"] = amount
        })
{
    public decimal Amount => Value;
}
