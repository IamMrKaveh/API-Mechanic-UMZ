using Domain.Common.ValueObjects;
using Domain.Wallet.Enums;

namespace Tests.DomainTest.Wallet;

public class WalletCreditDebitTests
{
    private static string UniqueKey() => Guid.NewGuid().ToString();

    [Fact]
    public void Credit_WithValidAmount_ShouldIncreaseBalance()
    {
        var wallet = new WalletBuilder().Build();

        wallet.Credit(
            Money.FromDecimal(500_000m),
            WalletTransactionType.ManualCredit,
            WalletReferenceType.Manual,
            1,
            UniqueKey());

        wallet.CurrentBalance.Should().Be(500_000m);
    }

    [Fact]
    public void Credit_ShouldRaiseWalletCreditedEvent()
    {
        var wallet = new WalletBuilder().Build();
        wallet.ClearDomainEvents();

        wallet.Credit(
            Money.FromDecimal(100_000m),
            WalletTransactionType.ManualCredit,
            WalletReferenceType.Manual,
            1,
            UniqueKey());

        wallet.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "WalletCreditedEvent");
    }

    [Fact]
    public void Credit_ShouldAddLedgerEntry()
    {
        var wallet = new WalletBuilder().Build();

        wallet.Credit(
            Money.FromDecimal(200_000m),
            WalletTransactionType.ManualCredit,
            WalletReferenceType.Manual,
            1,
            UniqueKey());

        wallet.PendingLedgerEntries.Should().HaveCount(2);
    }

    [Fact]
    public void Credit_WithZeroAmount_ShouldThrowDomainException()
    {
        var wallet = new WalletBuilder().Build();

        var act = () => wallet.Credit(
            Money.FromDecimal(0),
            WalletTransactionType.ManualCredit,
            WalletReferenceType.Manual,
            1,
            UniqueKey());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Debit_WithSufficientBalance_ShouldDecreaseBalance()
    {
        var wallet = new WalletBuilder().WithBalance(1_000_000m).Build();
        var balanceBefore = wallet.CurrentBalance;

        wallet.Debit(
            Money.FromDecimal(300_000m),
            WalletTransactionType.OrderPayment,
            WalletReferenceType.Order,
            1,
            UniqueKey());

        wallet.CurrentBalance.Should().Be(balanceBefore - 300_000m);
    }

    [Fact]
    public void Debit_ShouldRaiseWalletDebitedEvent()
    {
        var wallet = new WalletBuilder().WithBalance(1_000_000m).Build();
        wallet.ClearDomainEvents();

        wallet.Debit(
            Money.FromDecimal(100_000m),
            WalletTransactionType.OrderPayment,
            WalletReferenceType.Order,
            1,
            UniqueKey());

        wallet.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "WalletDebitedEvent");
    }

    [Fact]
    public void Debit_WithInsufficientBalance_ShouldThrowException()
    {
        var wallet = new WalletBuilder().WithBalance(100m).Build();

        var act = () => wallet.Debit(
            Money.FromDecimal(1_000_000m),
            WalletTransactionType.OrderPayment,
            WalletReferenceType.Order,
            1,
            UniqueKey());

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Debit_WithZeroAmount_ShouldThrowDomainException()
    {
        var wallet = new WalletBuilder().WithBalance(1_000_000m).Build();

        var act = () => wallet.Debit(
            Money.FromDecimal(0m),
            WalletTransactionType.OrderPayment,
            WalletReferenceType.Order,
            1,
            UniqueKey());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Credit_MultipleTimes_ShouldAccumulateBalance()
    {
        var wallet = new WalletBuilder().Build();

        wallet.Credit(Money.FromDecimal(500_000m), WalletTransactionType.ManualCredit, WalletReferenceType.Manual, 1, UniqueKey());
        wallet.Credit(Money.FromDecimal(300_000m), WalletTransactionType.ManualCredit, WalletReferenceType.Manual, 2, UniqueKey());

        wallet.CurrentBalance.Should().Be(800_000m);
    }

    [Fact]
    public void Credit_WhenWalletSuspended_ShouldThrowDomainException()
    {
        var wallet = new WalletBuilder().BuildSuspended();

        var act = () => wallet.Credit(
            Money.FromDecimal(100_000m),
            WalletTransactionType.ManualCredit,
            WalletReferenceType.Manual,
            1,
            UniqueKey());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Debit_WhenWalletSuspended_ShouldThrowDomainException()
    {
        var wallet = new WalletBuilder().WithBalance(1_000_000m).BuildSuspended();

        var act = () => wallet.Debit(
            Money.FromDecimal(100_000m),
            WalletTransactionType.OrderPayment,
            WalletReferenceType.Order,
            1,
            UniqueKey());

        act.Should().Throw<DomainException>();
    }
}