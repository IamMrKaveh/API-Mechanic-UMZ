using Domain.Common.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Exceptions;

public sealed class InsufficientWalletBalanceException(WalletId walletId, Money requested, Money available) : Exception($"Wallet '{walletId}' has insufficient balance. Requested: {requested.Amount} {requested.Currency}, Available: {available.Amount} {available.Currency}.")
{
}