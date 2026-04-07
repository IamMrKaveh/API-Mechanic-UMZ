using Domain.Common.Exceptions;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Exceptions;

public sealed class WalletInactiveException : DomainException
{
    public WalletId WalletId { get; }

    public override string ErrorCode => "WALLET_INACTIVE";

    public WalletInactiveException(WalletId walletId)
        : base($"کیف پول '{walletId}' غیرفعال است و قادر به پردازش تراکنش نیست.")
    {
        WalletId = walletId;
    }
}