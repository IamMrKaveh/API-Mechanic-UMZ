using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Exceptions;

public sealed class WalletInactiveException(WalletId walletId) : Exception($"Wallet '{walletId}' is inactive and cannot process transactions.")
{
}