namespace Domain.Wallet.Exceptions;

public sealed class InvalidWalletAmountException(decimal amount) : DomainException($"مبلغ تراکنش کیف پول '{amount}' نامعتبر است. مبلغ باید بزرگ‌تر از صفر باشد.")
{
    public decimal Amount { get; } = amount;
}