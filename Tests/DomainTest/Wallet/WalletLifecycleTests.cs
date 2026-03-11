using Domain.Wallet.Enums;

namespace Tests.DomainTest.Wallet;

public class WalletLifecycleTests
{
    [Fact]
    public void Suspend_WhenActive_ShouldSetStatusToSuspended()
    {
        var wallet = new WalletBuilder().Build();

        wallet.Suspend("تخلف مشکوک");

        wallet.Status.Should().Be(WalletStatus.Suspended);
        wallet.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Suspend_WhenAlreadySuspended_ShouldNotThrow()
    {
        var wallet = new WalletBuilder().BuildSuspended();

        var act = () => wallet.Suspend("مجدد");

        act.Should().NotThrow();
    }

    [Fact]
    public void Suspend_ShouldRaiseWalletStatusChangedEvent()
    {
        var wallet = new WalletBuilder().Build();
        wallet.ClearDomainEvents();

        wallet.Suspend("دلیل");

        wallet.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "WalletStatusChangedEvent");
    }

    [Fact]
    public void Reactivate_WhenSuspended_ShouldSetStatusToActive()
    {
        var wallet = new WalletBuilder().BuildSuspended();

        wallet.Reactivate();

        wallet.Status.Should().Be(WalletStatus.Active);
        wallet.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reactivate_WhenAlreadyActive_ShouldNotThrow()
    {
        var wallet = new WalletBuilder().Build();

        var act = () => wallet.Reactivate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Reactivate_ShouldRaiseWalletStatusChangedEvent()
    {
        var wallet = new WalletBuilder().BuildSuspended();
        wallet.ClearDomainEvents();

        wallet.Reactivate();

        wallet.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "WalletStatusChangedEvent");
    }

    [Fact]
    public void Close_WhenBalanceIsZero_ShouldSetStatusToClosed()
    {
        var wallet = new WalletBuilder().Build();

        wallet.Close();

        wallet.Status.Should().Be(WalletStatus.Closed);
    }

    [Fact]
    public void Close_WhenHasBalance_ShouldThrowDomainException()
    {
        var wallet = new WalletBuilder().WithBalance(100_000m).Build();

        var act = () => wallet.Close();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Close_WhenAlreadyClosed_ShouldNotThrow()
    {
        var wallet = new WalletBuilder().Build();
        wallet.Close();

        var act = () => wallet.Close();

        act.Should().NotThrow();
    }

    [Fact]
    public void Close_ShouldRaiseWalletStatusChangedEvent()
    {
        var wallet = new WalletBuilder().Build();
        wallet.ClearDomainEvents();

        wallet.Close();

        wallet.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "WalletStatusChangedEvent");
    }

    [Fact]
    public void Suspend_WhenClosed_ShouldThrowDomainException()
    {
        var wallet = new WalletBuilder().Build();
        wallet.Close();

        var act = () => wallet.Suspend("تخلف");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reactivate_WhenClosed_ShouldThrowDomainException()
    {
        var wallet = new WalletBuilder().Build();
        wallet.Close();

        var act = () => wallet.Reactivate();

        act.Should().Throw<DomainException>();
    }
}