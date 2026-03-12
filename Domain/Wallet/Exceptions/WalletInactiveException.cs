namespace Domain.Wallet.Exceptions;

public sealed class WalletInactiveException(WalletId walletId) : DomainException($"کیف پول '{walletId}' غیرفعال است و قادر به پردازش تراکنش نیست.")
{
    public WalletId WalletId { get; } = walletId;
}