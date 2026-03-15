using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Results;

public sealed record WalletBalanceResult(
    WalletId WalletId,
    UserId OwnerId,
    Money TotalBalance,
    Money ReservedBalance,
    Money AvailableBalance,
    bool IsActive);