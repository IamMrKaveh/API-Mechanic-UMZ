using Domain.User.ValueObjects;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Wallet.Events;

public class WalletEventsTests
{
    [Fact]
    public void WalletCreatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var walletId = WalletId.NewId();
        var ownerId = UserId.NewId();

        var sut = new WalletCreatedEvent(walletId, ownerId, "IRT");

        sut.WalletId.ShouldBe(walletId);
        sut.OwnerId.ShouldBe(ownerId);
        sut.Currency.ShouldBe("IRT");
    }

    [Fact]
    public void WalletCreditedEvent_ExposesConstructorArgumentsAndOwnerIdAlias()
    {
        var walletId = WalletId.NewId();
        var userId = UserId.NewId();
        var amount = Money.Create(100m, "IRT");
        var newBalance = Money.Create(500m, "IRT");

        var sut = new WalletCreditedEvent(walletId, userId, amount, newBalance, "desc", "ref", "idem", "corr");

        sut.WalletId.ShouldBe(walletId);
        sut.UserId.ShouldBe(userId);
        sut.OwnerId.ShouldBe(userId);
        sut.Amount.ShouldBe(amount);
        sut.NewBalance.ShouldBe(newBalance);
        sut.Description.ShouldBe("desc");
        sut.ReferenceId.ShouldBe("ref");
        sut.IdempotencyKey.ShouldBe("idem");
        sut.CorrelationId.ShouldBe("corr");
    }

    [Fact]
    public void WalletCreditedEvent_WithOptionalArgumentsOmitted_StoresNull()
    {
        var sut = new WalletCreditedEvent(WalletId.NewId(), UserId.NewId(), Money.Create(1m, "IRT"), Money.Create(1m, "IRT"), "d", "r");

        sut.IdempotencyKey.ShouldBeNull();
        sut.CorrelationId.ShouldBeNull();
    }

    [Fact]
    public void WalletDebitedEvent_ExposesConstructorArgumentsAndOwnerIdAlias()
    {
        var walletId = WalletId.NewId();
        var userId = UserId.NewId();
        var amount = Money.Create(50m, "IRT");
        var newBalance = Money.Create(450m, "IRT");

        var sut = new WalletDebitedEvent(walletId, userId, amount, newBalance, "desc", "ref");

        sut.WalletId.ShouldBe(walletId);
        sut.UserId.ShouldBe(userId);
        sut.OwnerId.ShouldBe(userId);
        sut.Amount.ShouldBe(amount);
        sut.NewBalance.ShouldBe(newBalance);
        sut.Description.ShouldBe("desc");
        sut.ReferenceId.ShouldBe("ref");
    }

    [Fact]
    public void WalletDebitRequestCreatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var walletId = WalletId.NewId();
        var ownerId = UserId.NewId();
        var requestId = WalletDebitRequestId.NewId();
        var amount = Money.Create(100m, "IRT");
        var requestedBy = UserId.NewId();

        var sut = new WalletDebitRequestCreatedEvent(walletId, ownerId, requestId, amount, "reason", requestedBy);

        sut.WalletId.ShouldBe(walletId);
        sut.OwnerId.ShouldBe(ownerId);
        sut.RequestId.ShouldBe(requestId);
        sut.Amount.ShouldBe(amount);
        sut.Reason.ShouldBe("reason");
        sut.RequestedBy.ShouldBe(requestedBy);
    }

    [Fact]
    public void WalletDebitRequestApprovedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var walletId = WalletId.NewId();
        var ownerId = UserId.NewId();
        var requestId = WalletDebitRequestId.NewId();
        var approvedBy = UserId.NewId();
        var amount = Money.Create(100m, "IRT");

        var sut = new WalletDebitRequestApprovedEvent(walletId, ownerId, requestId, amount, approvedBy);

        sut.WalletId.ShouldBe(walletId);
        sut.OwnerId.ShouldBe(ownerId);
        sut.RequestId.ShouldBe(requestId);
        sut.Amount.ShouldBe(amount);
        sut.ApprovedBy.ShouldBe(approvedBy);
    }

    [Fact]
    public void WalletDebitRequestRejectedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var walletId = WalletId.NewId();
        var ownerId = UserId.NewId();
        var requestId = WalletDebitRequestId.NewId();
        var rejectedBy = UserId.NewId();
        var amount = Money.Create(100m, "IRT");

        var sut = new WalletDebitRequestRejectedEvent(walletId, ownerId, requestId, amount, rejectedBy, "no funds");

        sut.WalletId.ShouldBe(walletId);
        sut.OwnerId.ShouldBe(ownerId);
        sut.RequestId.ShouldBe(requestId);
        sut.Amount.ShouldBe(amount);
        sut.RejectedBy.ShouldBe(rejectedBy);
        sut.RejectionReason.ShouldBe("no funds");
    }

    [Fact]
    public void WalletDebitRequestRejectedEvent_WithNullRejectionReason_StoresNull()
    {
        var sut = new WalletDebitRequestRejectedEvent(WalletId.NewId(), UserId.NewId(), WalletDebitRequestId.NewId(), Money.Create(1m, "IRT"), UserId.NewId(), null);

        sut.RejectionReason.ShouldBeNull();
    }

    [Fact]
    public void WalletDebitRequestCancelledEvent_ExposesConstructorArgumentsAsProperties()
    {
        var walletId = WalletId.NewId();
        var ownerId = UserId.NewId();
        var requestId = WalletDebitRequestId.NewId();
        var cancelledBy = UserId.NewId();
        var amount = Money.Create(100m, "IRT");

        var sut = new WalletDebitRequestCancelledEvent(walletId, ownerId, requestId, amount, cancelledBy);

        sut.WalletId.ShouldBe(walletId);
        sut.OwnerId.ShouldBe(ownerId);
        sut.RequestId.ShouldBe(requestId);
        sut.Amount.ShouldBe(amount);
        sut.CancelledBy.ShouldBe(cancelledBy);
    }

    [Fact]
    public void WalletReservationCreatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var walletId = WalletId.NewId();
        var ownerId = UserId.NewId();
        var reservationId = WalletReservationId.NewId();
        var amount = Money.Create(100m, "IRT");

        var sut = new WalletReservationCreatedEvent(walletId, ownerId, reservationId, amount, "purpose");

        sut.WalletId.ShouldBe(walletId);
        sut.OwnerId.ShouldBe(ownerId);
        sut.ReservationId.ShouldBe(reservationId);
        sut.Amount.ShouldBe(amount);
        sut.Purpose.ShouldBe("purpose");
    }

    [Fact]
    public void WalletReservationReleasedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var walletId = WalletId.NewId();
        var ownerId = UserId.NewId();
        var reservationId = WalletReservationId.NewId();
        var amount = Money.Create(100m, "IRT");

        var sut = new WalletReservationReleasedEvent(walletId, ownerId, reservationId, amount);

        sut.WalletId.ShouldBe(walletId);
        sut.OwnerId.ShouldBe(ownerId);
        sut.ReservationId.ShouldBe(reservationId);
        sut.Amount.ShouldBe(amount);
    }

    [Fact]
    public void WalletFrozenEvent_ExposesConstructorArgumentsAsProperties()
    {
        var walletId = WalletId.NewId();
        var ownerId = UserId.NewId();
        var frozenBy = UserId.NewId();

        var sut = new WalletFrozenEvent(walletId, ownerId, "suspicious activity", frozenBy);

        sut.WalletId.ShouldBe(walletId);
        sut.OwnerId.ShouldBe(ownerId);
        sut.Reason.ShouldBe("suspicious activity");
        sut.FrozenBy.ShouldBe(frozenBy);
    }

    [Fact]
    public void WalletUnfrozenEvent_ExposesConstructorArgumentsAsProperties()
    {
        var walletId = WalletId.NewId();
        var ownerId = UserId.NewId();
        var unfrozenBy = UserId.NewId();

        var sut = new WalletUnfrozenEvent(walletId, ownerId, unfrozenBy, "cleared review");

        sut.WalletId.ShouldBe(walletId);
        sut.OwnerId.ShouldBe(ownerId);
        sut.UnfrozenBy.ShouldBe(unfrozenBy);
        sut.Reason.ShouldBe("cleared review");
    }
}
