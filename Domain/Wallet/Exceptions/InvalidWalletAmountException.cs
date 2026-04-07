using Domain.Common.Exceptions;

namespace Domain.Wallet.Exceptions;

public sealed class InvalidWalletAmountException : DomainException
{
    public decimal Amount { get; }

    public override string ErrorCode => "INVALID_WALLET_AMOUNT";

    public InvalidWalletAmountException(decimal amount)
        : base($"مبلغ تراکنش کیف پول '{amount}' نامعتبر است. مبلغ باید بزرگ‌تر از صفر باشد.")
    {
        Amount = amount;
    }
}