using Domain.Common.Exceptions;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Exceptions;

public sealed class InsufficientWalletBalanceException : DomainException
{
    public WalletId WalletId { get; }
    public Money Requested { get; }
    public Money Available { get; }

    public override string ErrorCode => "INSUFFICIENT_WALLET_BALANCE";

    public InsufficientWalletBalanceException(WalletId walletId, Money requested, Money available)
        : base($"کیف پول '{walletId}' موجودی کافی ندارد. درخواستی: {requested.Amount:N0} {requested.Currency}، موجود: {available.Amount:N0} {available.Currency}.")
    {
        WalletId = walletId;
        Requested = requested;
        Available = available;
    }
}