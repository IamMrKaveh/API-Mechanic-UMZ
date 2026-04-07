using Domain.Common.Exceptions;
using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Exceptions;

public sealed class WalletNotFoundException : DomainException
{
    public WalletId? WalletId { get; }
    public UserId? OwnerId { get; }

    public override string ErrorCode => "WALLET_NOT_FOUND";

    public WalletNotFoundException(WalletId walletId)
        : base($"کیف پول با شناسه '{walletId}' یافت نشد.")
    {
        WalletId = walletId;
    }

    public WalletNotFoundException(UserId ownerId)
        : base($"کیف پول کاربر '{ownerId}' یافت نشد.")
    {
        OwnerId = ownerId;
    }
}