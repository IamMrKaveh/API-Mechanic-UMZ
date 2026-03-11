using Domain.Common.ValueObjects;
using Domain.Wallet.Enums;

namespace Tests.Builders.Wallet;

public class WalletBuilder
{
    private int _userId = 1;
    private decimal _initialBalance = 0m;

    public WalletBuilder WithUserId(int userId)
    {
        _userId = userId;
        return this;
    }

    public WalletBuilder WithBalance(decimal balance)
    {
        _initialBalance = balance;
        return this;
    }

    public Domain.Wallet.Aggregates.Wallet Build()
    {
        var wallet = Domain.Wallet.Aggregates.Wallet.Create(_userId);

        if (_initialBalance > 0)
        {
            wallet.Credit(
                Money.FromDecimal(_initialBalance),
                WalletTransactionType.ManualCredit,
                WalletReferenceType.Manual,
                1,
                Guid.NewGuid().ToString());
        }

        return wallet;
    }

    public Domain.Wallet.Aggregates.Wallet BuildSuspended()
    {
        var wallet = Build();
        wallet.Suspend("تست");
        return wallet;
    }
}