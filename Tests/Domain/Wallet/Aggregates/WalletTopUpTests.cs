using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.Exceptions;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Wallet.Aggregates;

public class WalletTopUpTests
{
    private const decimal MinimumAmount = 10_000m;

    private static Money Rial(decimal amount) => Money.Create(amount, "IRT");

    // ---------- Initiate factory ----------

    [Fact]
    public void Initiate_WithValidInput_ReturnsPendingTopUp()
    {
        var userId = UserId.NewId();
        var amount = Rial(50_000m);

        var sut = new WalletTopUpBuilder()
            .WithUserId(userId)
            .WithAmount(amount)
            .WithGateway("zarinpal")
            .Build();

        sut.Id.ShouldNotBeNull();
        sut.UserId.ShouldBe(userId);
        sut.Amount.ShouldBe(amount);
        sut.Gateway.ShouldBe("zarinpal");
        sut.Status.ShouldBe(WalletTopUpStatus.Pending);
        sut.GatewayAuthority.ShouldBeNull();
        sut.GatewayRefId.ShouldBeNull();
        sut.CompletedAt.ShouldBeNull();
        sut.FailureReason.ShouldBeNull();
    }

    [Fact]
    public void Initiate_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new WalletTopUpBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Initiate_RaisesExactlyOneWalletTopUpInitiatedEvent()
    {
        var sut = new WalletTopUpBuilder().Build();

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WalletTopUpInitiatedEvent>();
        evt.TopUpId.ShouldBe(sut.Id);
        evt.UserId.ShouldBe(sut.UserId);
        evt.Amount.ShouldBe(sut.Amount);
        evt.Gateway.ShouldBe(sut.Gateway);
    }

    [Fact]
    public void Initiate_IncrementsVersionToOne()
    {
        new WalletTopUpBuilder().Build().Version.ShouldBe(1);
    }

    [Fact]
    public void Initiate_WithAmountEqualToMinimum_Succeeds()
    {
        var sut = new WalletTopUpBuilder().WithAmount(Rial(MinimumAmount)).Build();

        sut.Amount.Amount.ShouldBe(MinimumAmount);
        sut.Status.ShouldBe(WalletTopUpStatus.Pending);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9_999)]
    public void Initiate_WithAmountBelowMinimum_ThrowsInvalidTopUpAmountException(decimal amount)
    {
        Should.Throw<InvalidTopUpAmountException>(() =>
            WalletTopUp.Initiate(UserId.NewId(), Rial(amount), "zarinpal"));
    }

    [Fact]
    public void Initiate_WithNullUserId_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            WalletTopUp.Initiate(null!, Rial(50_000m), "zarinpal"));
    }

    [Fact]
    public void Initiate_WithNullAmount_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            WalletTopUp.Initiate(UserId.NewId(), null!, "zarinpal"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Initiate_WithBlankGateway_ThrowsDomainException(string? gateway)
    {
        Should.Throw<DomainException>(() =>
            WalletTopUp.Initiate(UserId.NewId(), Rial(50_000m), gateway!));
    }

    // ---------- MarkAuthorityIssued ----------

    [Fact]
    public void MarkAuthorityIssued_OnPending_StoresAuthorityWithoutChangingStatus()
    {
        var sut = new WalletTopUpBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.MarkAuthorityIssued("AUTH-XYZ-123");

        sut.GatewayAuthority.ShouldBe("AUTH-XYZ-123");
        sut.Status.ShouldBe(WalletTopUpStatus.Pending);
        sut.CompletedAt.ShouldBeNull();
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkAuthorityIssued_WithBlankAuthority_ThrowsDomainException(string? authority)
    {
        var sut = new WalletTopUpBuilder().Build();

        Should.Throw<DomainException>(() => sut.MarkAuthorityIssued(authority!));
    }

    [Fact]
    public void MarkAuthorityIssued_OnSucceeded_ThrowsDomainException()
    {
        var sut = new WalletTopUpBuilder().Build();
        sut.MarkSucceeded("REF-1");

        Should.Throw<DomainException>(() => sut.MarkAuthorityIssued("AUTH"));
    }

    [Fact]
    public void MarkAuthorityIssued_OnFailed_ThrowsDomainException()
    {
        var sut = new WalletTopUpBuilder().Build();
        sut.MarkFailed("timeout");

        Should.Throw<DomainException>(() => sut.MarkAuthorityIssued("AUTH"));
    }

    // ---------- MarkSucceeded ----------

    [Fact]
    public void MarkSucceeded_OnPending_TransitionsToSucceededAndRaisesEvent()
    {
        var sut = new WalletTopUpBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;
        var before = DateTime.UtcNow.AddSeconds(-1);

        sut.MarkSucceeded("REF-999");

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.Status.ShouldBe(WalletTopUpStatus.Succeeded);
        sut.GatewayRefId.ShouldBe("REF-999");
        sut.CompletedAt.ShouldNotBeNull();
        sut.CompletedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        sut.CompletedAt.Value.ShouldBeLessThanOrEqualTo(after);
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WalletTopUpSucceededEvent>();
        evt.TopUpId.ShouldBe(sut.Id);
        evt.UserId.ShouldBe(sut.UserId);
        evt.Amount.ShouldBe(sut.Amount);
        evt.GatewayRefId.ShouldBe("REF-999");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkSucceeded_WithBlankRefId_ThrowsDomainException(string? refId)
    {
        var sut = new WalletTopUpBuilder().Build();

        Should.Throw<DomainException>(() => sut.MarkSucceeded(refId!));
    }

    [Fact]
    public void MarkSucceeded_AlreadySucceeded_ThrowsDomainException()
    {
        var sut = new WalletTopUpBuilder().Build();
        sut.MarkSucceeded("REF-1");

        Should.Throw<DomainException>(() => sut.MarkSucceeded("REF-2"));
    }

    [Fact]
    public void MarkSucceeded_AfterFailed_ThrowsDomainException()
    {
        var sut = new WalletTopUpBuilder().Build();
        sut.MarkFailed("reason");

        Should.Throw<DomainException>(() => sut.MarkSucceeded("REF"));
    }

    [Fact]
    public void MarkSucceeded_AfterCancelled_ThrowsDomainException()
    {
        var sut = new WalletTopUpBuilder().Build();
        sut.MarkCancelled("user");

        Should.Throw<DomainException>(() => sut.MarkSucceeded("REF"));
    }

    // ---------- MarkFailed ----------

    [Fact]
    public void MarkFailed_OnPending_TransitionsToFailedAndRaisesEvent()
    {
        var sut = new WalletTopUpBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.MarkFailed("gateway timeout");

        sut.Status.ShouldBe(WalletTopUpStatus.Failed);
        sut.FailureReason.ShouldBe("gateway timeout");
        sut.CompletedAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WalletTopUpFailedEvent>();
        evt.TopUpId.ShouldBe(sut.Id);
        evt.UserId.ShouldBe(sut.UserId);
        evt.Reason.ShouldBe("gateway timeout");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkFailed_WithBlankReason_UsesUnspecifiedMarker(string? reason)
    {
        var sut = new WalletTopUpBuilder().Build();

        sut.MarkFailed(reason!);

        sut.Status.ShouldBe(WalletTopUpStatus.Failed);
        sut.FailureReason.ShouldBe("[UNSPECIFIED]");
        var evt = sut.DomainEvents.OfType<WalletTopUpFailedEvent>().Single();
        evt.Reason.ShouldBe("[UNSPECIFIED]");
    }

    [Fact]
    public void MarkFailed_AlreadyFailed_ThrowsDomainException()
    {
        var sut = new WalletTopUpBuilder().Build();
        sut.MarkFailed("first");

        Should.Throw<DomainException>(() => sut.MarkFailed("second"));
    }

    [Fact]
    public void MarkFailed_AfterSucceeded_ThrowsDomainException()
    {
        var sut = new WalletTopUpBuilder().Build();
        sut.MarkSucceeded("REF");

        Should.Throw<DomainException>(() => sut.MarkFailed("reason"));
    }

    // ---------- MarkCancelled ----------

    [Fact]
    public void MarkCancelled_OnPending_TransitionsToCancelledAndRaisesFailedEvent()
    {
        var sut = new WalletTopUpBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.MarkCancelled("user requested");

        sut.Status.ShouldBe(WalletTopUpStatus.Cancelled);
        sut.FailureReason.ShouldBe("user requested");
        sut.CompletedAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WalletTopUpFailedEvent>();
        evt.Reason.ShouldBe("user requested");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkCancelled_WithBlankReason_UsesUnspecifiedMarker(string? reason)
    {
        var sut = new WalletTopUpBuilder().Build();

        sut.MarkCancelled(reason!);

        sut.FailureReason.ShouldBe("[UNSPECIFIED]");
    }

    [Fact]
    public void MarkCancelled_AlreadyCancelled_ThrowsDomainException()
    {
        var sut = new WalletTopUpBuilder().Build();
        sut.MarkCancelled("first");

        Should.Throw<DomainException>(() => sut.MarkCancelled("second"));
    }

    [Fact]
    public void MarkCancelled_AfterSucceeded_ThrowsDomainException()
    {
        var sut = new WalletTopUpBuilder().Build();
        sut.MarkSucceeded("REF");

        Should.Throw<DomainException>(() => sut.MarkCancelled("reason"));
    }

    [Fact]
    public void MarkCancelled_AfterFailed_ThrowsDomainException()
    {
        var sut = new WalletTopUpBuilder().Build();
        sut.MarkFailed("reason");

        Should.Throw<DomainException>(() => sut.MarkCancelled("reason"));
    }
}
