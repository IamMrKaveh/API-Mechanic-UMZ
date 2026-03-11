using Domain.Wallet.Enums;

namespace Tests.DomainTest.Wallet;

public class WalletCreationTests
{
    [Fact]
    public void Create_WithValidUserId_ShouldCreateActiveWallet()
    {
        var wallet = Domain.Wallet.Aggregates.Wallet.Create(1);

        wallet.Should().NotBeNull();
        wallet.UserId.Should().Be(1);
        wallet.IsActive.Should().BeTrue();
        wallet.Status.Should().Be(WalletStatus.Active);
    }

    [Fact]
    public void Create_ShouldHaveZeroInitialBalance()
    {
        var wallet = Domain.Wallet.Aggregates.Wallet.Create(1);

        wallet.CurrentBalance.Should().Be(0);
        wallet.ReservedBalance.Should().Be(0);
        wallet.AvailableBalance.Should().Be(0);
    }

    [Fact]
    public void Create_WithZeroUserId_ShouldThrowException()
    {
        var act = () => Domain.Wallet.Aggregates.Wallet.Create(0);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var wallet = Domain.Wallet.Aggregates.Wallet.Create(1);

        wallet.CreatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void Create_ShouldHaveNoReservations()
    {
        var wallet = Domain.Wallet.Aggregates.Wallet.Create(1);

        wallet.Reservations.Should().BeEmpty();
    }

    [Fact]
    public void HasSufficientBalance_WhenBalanceSufficient_ShouldReturnTrue()
    {
        var wallet = new WalletBuilder().WithBalance(1_000_000m).Build();

        wallet.HasSufficientBalance(500_000m).Should().BeTrue();
    }

    [Fact]
    public void HasSufficientBalance_WhenBalanceInsufficient_ShouldReturnFalse()
    {
        var wallet = new WalletBuilder().WithBalance(100m).Build();

        wallet.HasSufficientBalance(500_000m).Should().BeFalse();
    }

    [Fact]
    public void AvailableBalance_ShouldBeCurrentBalanceMinusReservedBalance()
    {
        var wallet = new WalletBuilder().WithBalance(1_000_000m).Build();

        var expectedAvailable = wallet.CurrentBalance - wallet.ReservedBalance;

        wallet.AvailableBalance.Should().Be(expectedAvailable);
    }
}