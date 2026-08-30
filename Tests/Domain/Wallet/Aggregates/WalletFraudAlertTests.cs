using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Wallet.Aggregates;

public class WalletFraudAlertTests
{
    // ---------- Creation (Raise factory) ----------

    [Fact]
    public void Raise_WithValidInput_ReturnsInitializedFraudAlert()
    {
        var walletId = WalletId.NewId();
        var userId = UserId.NewId();

        var sut = new WalletFraudAlertBuilder()
            .WithWalletId(walletId)
            .WithUserId(userId)
            .WithRuleName("HighAmountRule")
            .WithSeverity(FraudAlertSeverity.High)
            .WithDescription("Suspicious spike detected")
            .WithMetadata("{\"amount\":100000}")
            .Build();

        sut.Id.ShouldNotBeNull();
        sut.WalletId.ShouldBe(walletId);
        sut.UserId.ShouldBe(userId);
        sut.RuleName.ShouldBe("HighAmountRule");
        sut.Severity.ShouldBe(FraudAlertSeverity.High);
        sut.Description.ShouldBe("Suspicious spike detected");
        sut.Metadata.ShouldBe("{\"amount\":100000}");
        sut.Status.ShouldBe(FraudAlertStatus.Open);
        sut.ReviewedAt.ShouldBeNull();
        sut.ReviewedBy.ShouldBeNull();
        sut.ReviewNote.ShouldBeNull();
    }

    [Fact]
    public void Raise_SetsTriggeredAtCreatedAtAndUpdatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new WalletFraudAlertBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.TriggeredAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.TriggeredAt.ShouldBeLessThanOrEqualTo(after);
        sut.CreatedAt.ShouldBe(sut.TriggeredAt);
        sut.UpdatedAt.ShouldBe(sut.TriggeredAt);
    }

    [Fact]
    public void Raise_WithoutMetadata_LeavesMetadataNull()
    {
        var sut = new WalletFraudAlertBuilder().WithMetadata(null).Build();

        sut.Metadata.ShouldBeNull();
    }

    [Fact]
    public void Raise_RaisesExactlyOneWalletFraudAlertRaisedEvent()
    {
        var walletId = WalletId.NewId();
        var userId = UserId.NewId();

        var sut = new WalletFraudAlertBuilder()
            .WithWalletId(walletId)
            .WithUserId(userId)
            .WithRuleName("R")
            .WithSeverity(FraudAlertSeverity.Critical)
            .WithDescription("D")
            .Build();

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WalletFraudAlertRaisedEvent>();
        evt.AlertId.ShouldBe(sut.Id);
        evt.WalletId.ShouldBe(walletId);
        evt.UserId.ShouldBe(userId);
        evt.RuleName.ShouldBe("R");
        evt.Severity.ShouldBe(FraudAlertSeverity.Critical);
        evt.Description.ShouldBe("D");
        evt.TriggeredAt.ShouldBe(sut.TriggeredAt);
    }

    [Fact]
    public void Raise_IncrementsVersionToOne()
    {
        new WalletFraudAlertBuilder().Build().Version.ShouldBe(1);
    }

    [Fact]
    public void Raise_WithNullWalletId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            WalletFraudAlert.Raise(null!, UserId.NewId(), "R", FraudAlertSeverity.Low, "D"));
    }

    [Fact]
    public void Raise_WithNullUserId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            WalletFraudAlert.Raise(WalletId.NewId(), null!, "R", FraudAlertSeverity.Low, "D"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Raise_WithBlankRuleName_ThrowsArgumentException(string? ruleName)
    {
        Should.Throw<ArgumentException>(() =>
            WalletFraudAlert.Raise(WalletId.NewId(), UserId.NewId(), ruleName!, FraudAlertSeverity.Low, "D"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Raise_WithBlankDescription_ThrowsArgumentException(string? description)
    {
        Should.Throw<ArgumentException>(() =>
            WalletFraudAlert.Raise(WalletId.NewId(), UserId.NewId(), "R", FraudAlertSeverity.Low, description!));
    }

    [Theory]
    [InlineData(FraudAlertSeverity.Low)]
    [InlineData(FraudAlertSeverity.Medium)]
    [InlineData(FraudAlertSeverity.High)]
    [InlineData(FraudAlertSeverity.Critical)]
    public void Raise_PreservesSeverity(FraudAlertSeverity severity)
    {
        var sut = new WalletFraudAlertBuilder().WithSeverity(severity).Build();

        sut.Severity.ShouldBe(severity);
    }

    // ---------- MarkAsReviewed ----------

    [Fact]
    public void MarkAsReviewed_OnOpenAlert_TransitionsToReviewedAndRaisesEvent()
    {
        var sut = new WalletFraudAlertBuilder().Build();
        sut.ClearDomainEvents();
        var reviewer = UserId.NewId();
        var versionBefore = sut.Version;
        var before = DateTime.UtcNow.AddSeconds(-1);

        sut.MarkAsReviewed(reviewer, "looked into it");

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.Status.ShouldBe(FraudAlertStatus.Reviewed);
        sut.ReviewedBy.ShouldBe(reviewer);
        sut.ReviewNote.ShouldBe("looked into it");
        sut.ReviewedAt.ShouldNotBeNull();
        sut.ReviewedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        sut.ReviewedAt.Value.ShouldBeLessThanOrEqualTo(after);
        sut.UpdatedAt.ShouldBe(sut.ReviewedAt.Value);
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WalletFraudAlertReviewedEvent>();
        evt.AlertId.ShouldBe(sut.Id);
        evt.ReviewedBy.ShouldBe(reviewer);
        evt.ReviewNote.ShouldBe("looked into it");
        evt.ReviewedAt.ShouldBe(sut.ReviewedAt.Value);
    }

    [Fact]
    public void MarkAsReviewed_WithNullNote_StoresNullReviewNote()
    {
        var sut = new WalletFraudAlertBuilder().Build();

        sut.MarkAsReviewed(UserId.NewId(), null);

        sut.ReviewNote.ShouldBeNull();
    }

    [Fact]
    public void MarkAsReviewed_WithNullReviewer_ThrowsArgumentNullException()
    {
        var sut = new WalletFraudAlertBuilder().Build();

        Should.Throw<ArgumentNullException>(() => sut.MarkAsReviewed(null!, "note"));
    }

    [Fact]
    public void MarkAsReviewed_OnAlreadyReviewedAlert_ThrowsDomainException()
    {
        var sut = new WalletFraudAlertBuilder().Build();
        sut.MarkAsReviewed(UserId.NewId(), "first");

        Should.Throw<DomainException>(() => sut.MarkAsReviewed(UserId.NewId(), "second"));
    }

    [Fact]
    public void MarkAsReviewed_OnDismissedAlert_ThrowsDomainException()
    {
        var sut = new WalletFraudAlertBuilder().Build();
        sut.Dismiss(UserId.NewId(), null);

        Should.Throw<DomainException>(() => sut.MarkAsReviewed(UserId.NewId(), "note"));
    }

    // ---------- Dismiss ----------

    [Fact]
    public void Dismiss_OnOpenAlert_TransitionsToDismissedAndRaisesEvent()
    {
        var sut = new WalletFraudAlertBuilder().Build();
        sut.ClearDomainEvents();
        var dismisser = UserId.NewId();
        var versionBefore = sut.Version;
        var before = DateTime.UtcNow.AddSeconds(-1);

        sut.Dismiss(dismisser, "false positive");

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.Status.ShouldBe(FraudAlertStatus.Dismissed);
        sut.ReviewedBy.ShouldBe(dismisser);
        sut.ReviewNote.ShouldBe("false positive");
        sut.ReviewedAt.ShouldNotBeNull();
        sut.ReviewedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        sut.ReviewedAt.Value.ShouldBeLessThanOrEqualTo(after);
        sut.UpdatedAt.ShouldBe(sut.ReviewedAt.Value);
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WalletFraudAlertDismissedEvent>();
        evt.AlertId.ShouldBe(sut.Id);
        evt.DismissedBy.ShouldBe(dismisser);
        evt.DismissNote.ShouldBe("false positive");
        evt.DismissedAt.ShouldBe(sut.ReviewedAt.Value);
    }

    [Fact]
    public void Dismiss_WithNullNote_StoresNullReviewNote()
    {
        var sut = new WalletFraudAlertBuilder().Build();

        sut.Dismiss(UserId.NewId(), null);

        sut.ReviewNote.ShouldBeNull();
    }

    [Fact]
    public void Dismiss_WithNullDismisser_ThrowsArgumentNullException()
    {
        var sut = new WalletFraudAlertBuilder().Build();

        Should.Throw<ArgumentNullException>(() => sut.Dismiss(null!, "note"));
    }

    [Fact]
    public void Dismiss_OnAlreadyDismissedAlert_ThrowsDomainException()
    {
        var sut = new WalletFraudAlertBuilder().Build();
        sut.Dismiss(UserId.NewId(), null);

        Should.Throw<DomainException>(() => sut.Dismiss(UserId.NewId(), null));
    }

    [Fact]
    public void Dismiss_OnReviewedAlert_ThrowsDomainException()
    {
        var sut = new WalletFraudAlertBuilder().Build();
        sut.MarkAsReviewed(UserId.NewId(), null);

        Should.Throw<DomainException>(() => sut.Dismiss(UserId.NewId(), null));
    }
}
