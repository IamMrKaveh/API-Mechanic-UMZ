namespace Domain.Wallet.Exceptions;

public sealed class InvalidWalletAmountException(decimal amount) : Exception($"Wallet transaction amount '{amount}' is invalid. Amount must be greater than zero.")
{
}