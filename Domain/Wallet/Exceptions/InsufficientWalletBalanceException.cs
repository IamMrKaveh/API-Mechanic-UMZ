namespace Domain.Wallet.Exceptions;

public sealed class InsufficientWalletBalanceException : DomainException
{
    public int UserId { get; }
    public decimal Available { get; }
    public decimal Required { get; }

    public InsufficientWalletBalanceException(int userId, decimal available, decimal required)
        : base($"موجودی کیف پول کافی نیست. موجودی: {available:N0}، مبلغ مورد نیاز: {required:N0}")
    {
        UserId = userId;
        Available = available;
        Required = required;
    }
}