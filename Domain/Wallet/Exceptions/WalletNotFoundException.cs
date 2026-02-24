namespace Domain.Wallet.Exceptions;

public sealed class WalletNotFoundException : DomainException
{
    public WalletNotFoundException(int userId)
        : base($"کیف پول کاربر {userId} یافت نشد.")
    {
    }
}