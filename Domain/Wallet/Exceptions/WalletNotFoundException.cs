namespace Domain.Wallet.Exceptions;

public sealed class WalletNotFoundException(int userId) : DomainException($"کیف پول کاربر {userId} یافت نشد.")
{
}