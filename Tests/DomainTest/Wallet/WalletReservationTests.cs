using Domain.Common.ValueObjects;

namespace Tests.DomainTest.Wallet;

public class WalletReservationTests
{
    private static string UniqueKey() => Guid.NewGuid().ToString();

    [Fact]
    public void Reserve_WithSufficientBalance_ShouldCreateReservation()
    {
        var wallet = new WalletBuilder().WithBalance(1_000_000m).Build();

        wallet.Reserve(Money.FromDecimal(500_000m), orderId: 1);

        wallet.Reservations.Should().HaveCount(1);
        wallet.ReservedBalance.Should().Be(500_000m);
    }

    [Fact]
    public void Reserve_ShouldDecreaseAvailableBalance()
    {
        var wallet = new WalletBuilder().WithBalance(1_000_000m).Build();
        var initialAvailable = wallet.AvailableBalance;

        wallet.Reserve(Money.FromDecimal(400_000m), orderId: 1);

        wallet.AvailableBalance.Should().Be(initialAvailable - 400_000m);
    }

    [Fact]
    public void Reserve_ShouldNotDecreaseCurrentBalance()
    {
        var wallet = new WalletBuilder().WithBalance(1_000_000m).Build();
        var balanceBefore = wallet.CurrentBalance;

        wallet.Reserve(Money.FromDecimal(400_000m), orderId: 1);

        wallet.CurrentBalance.Should().Be(balanceBefore);
    }

    [Fact]
    public void Reserve_ShouldRaiseWalletReservationCreatedEvent()
    {
        var wallet = new WalletBuilder().WithBalance(1_000_000m).Build();
        wallet.ClearDomainEvents();

        wallet.Reserve(Money.FromDecimal(200_000m), orderId: 1);

        wallet.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "WalletReservationCreatedEvent");
    }

    [Fact]
    public void Reserve_WithInsufficientBalance_ShouldThrowException()
    {
        var wallet = new WalletBuilder().WithBalance(100m).Build();

        var act = () => wallet.Reserve(Money.FromDecimal(500_000m), orderId: 1);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void CommitReservation_WithValidOrderId_ShouldDeductFromBalance()
    {
        var wallet = new WalletBuilder().WithBalance(1_000_000m).Build();
        wallet.Reserve(Money.FromDecimal(300_000m), orderId: 10);
        var balanceBefore = wallet.CurrentBalance;

        wallet.CommitReservation(orderId: 10, idempotencyKey: UniqueKey());

        wallet.CurrentBalance.Should().Be(balanceBefore - 300_000m);
        wallet.ReservedBalance.Should().Be(0);
    }

    [Fact]
    public void CommitReservation_ShouldRaiseReservationCommittedEvent()
    {
        var wallet = new WalletBuilder().WithBalance(500_000m).Build();
        wallet.Reserve(Money.FromDecimal(200_000m), orderId: 5);
        wallet.ClearDomainEvents();

        wallet.CommitReservation(5, UniqueKey());

        wallet.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "WalletReservationCommittedEvent");
    }

    [Fact]
    public void CommitReservation_WithNonExistentOrderId_ShouldThrowDomainException()
    {
        var wallet = new WalletBuilder().WithBalance(500_000m).Build();

        var act = () => wallet.CommitReservation(orderId: 999, idempotencyKey: UniqueKey());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ReleaseReservation_ShouldRestoreAvailableBalance()
    {
        var wallet = new WalletBuilder().WithBalance(1_000_000m).Build();
        wallet.Reserve(Money.FromDecimal(300_000m), orderId: 3);
        var availableBefore = wallet.AvailableBalance;

        wallet.ReleaseReservation(orderId: 3);

        wallet.AvailableBalance.Should().BeGreaterThan(availableBefore);
        wallet.ReservedBalance.Should().Be(0);
    }

    [Fact]
    public void ReleaseReservation_ShouldRaiseReservationReleasedEvent()
    {
        var wallet = new WalletBuilder().WithBalance(1_000_000m).Build();
        wallet.Reserve(Money.FromDecimal(100_000m), orderId: 7);
        wallet.ClearDomainEvents();

        wallet.ReleaseReservation(orderId: 7);

        wallet.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "WalletReservationReleasedEvent");
    }

    [Fact]
    public void ReleaseReservation_WithNonExistentOrderId_ShouldNotThrow()
    {
        var wallet = new WalletBuilder().WithBalance(500_000m).Build();

        var act = () => wallet.ReleaseReservation(orderId: 999);

        act.Should().NotThrow();
    }
}