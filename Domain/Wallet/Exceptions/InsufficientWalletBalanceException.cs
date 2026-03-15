using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Exceptions;

public sealed class InsufficientWalletBalanceException(WalletId walletId, Money requested, Money available) : DomainException($"کیف پول '{walletId}' موجودی کافی ندارد. درخواستی: {requested.Amount:N0} {requested.Currency}، موجود: {available.Amount:N0} {available.Currency}.")
{
    public WalletId WalletId { get; } = walletId;
    public Money Requested { get; } = requested;
    public Money Available { get; } = available;
}