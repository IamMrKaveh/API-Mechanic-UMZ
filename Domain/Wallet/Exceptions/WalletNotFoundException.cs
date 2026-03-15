using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Exceptions;

public sealed class WalletNotFoundException : DomainException
{
    public WalletNotFoundException(WalletId walletId)
        : base($"کیف پول با شناسه '{walletId}' یافت نشد.")
    {
    }

    public WalletNotFoundException(UserId ownerId)
        : base($"کیف پول کاربر '{ownerId}' یافت نشد.")
    {
    }
}