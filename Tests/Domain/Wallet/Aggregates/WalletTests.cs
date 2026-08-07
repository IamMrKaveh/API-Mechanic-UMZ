using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.Exceptions;
using Domain.Wallet.ValueObjects;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Domain.Wallet.Aggregates;

public class WalletTests
{
    private static Money Rial(decimal amount) => Money.Create(amount, "IRT");

    private static Wallets BuildFunded(decimal openingBalance)
    {
        var wallet = new WalletBuilder().Build();
        if (openingBalance > 0)
            wallet.Credit(Rial(openingBalance), "seed", "seed-ref");
        return wallet;
    }

    [Fact]
    public void Create_WithValidInput_ReturnsInitializedWallet()
    {
        var ownerId = UserId.NewId();

        var sut = new WalletBuilder().WithOwnerId(ownerId).WithCurrency("IRT").Build();

        sut.Id.ShouldNotBeNull();
        sut.OwnerId.ShouldBe(ownerId);
        sut.Balance.Amount.ShouldBe(0m);
        sut.Balance.Currency.ShouldBe("IRT");
        sut.IsActive.ShouldBeTrue();
        sut.FreezeReason.ShouldBeNull();
        sut.FrozenAt.ShouldBeNull();
        sut.FrozenBy.ShouldBeNull();
        sut.ActiveReservations.ShouldBeEmpty();
        sut.DebitRequests.ShouldBeEmpty();
    }

    [Fact]
    public void Create_SetsCreatedAtAndUpdatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new WalletBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        sut.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public void Create_ProducesWalletWithVersionOne()
    {
        new WalletBuilder().Build().Version.ShouldBe(1);
    }

    [Fact]
    public void Create_RaisesExactlyOneWalletCreatedEvent()
    {
        var sut = new WalletBuilder().Build();

        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<WalletCreatedEvent>();
    }

    [Fact]
    public void Create_WithCustomCurrency_UsesThatCurrency()
    {
        new WalletBuilder().WithCurrency("USD").Build().Balance.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Create_WithNullOwnerId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new WalletBuilder().WithOwnerId(null!).Build());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceCurrency_ThrowsArgumentException(string? currency)
    {
        Should.Throw<ArgumentException>(() => new WalletBuilder().WithCurrency(currency!).Build());
    }

    [Fact]
    public void Credit_WithPositiveAmount_IncreasesBalanceAndRaisesEvent()
    {
        var sut = new WalletBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Credit(Rial(100), "deposit", "ref-1");

        sut.Balance.Amount.ShouldBe(100m);
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.Single().ShouldBeOfType<WalletCreditedEvent>();
    }

    [Fact]
    public void Credit_DoesNotEnforceActiveGate_WorksOnFrozenWallet()
    {
        var sut = new WalletBuilder().Build();
        sut.Freeze("audit", UserId.NewId());

        Should.NotThrow(() => sut.Credit(Rial(50), "refund", "ref-refund"));
        sut.Balance.Amount.ShouldBe(50m);
    }

    [Fact]
    public void Credit_WithZeroAmount_ThrowsInvalidWalletAmountException()
    {
        var sut = new WalletBuilder().Build();

        Should.Throw<InvalidWalletAmountException>(() => sut.Credit(Rial(0), "d", "r"));
    }

    [Fact]
    public void Credit_WithNullAmount_ThrowsArgumentException()
    {
        var sut = new WalletBuilder().Build();

        Should.Throw<ArgumentException>(() => sut.Credit(null!, "d", "r"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Credit_WithNullOrWhitespaceDescription_ThrowsArgumentException(string? description)
    {
        var sut = new WalletBuilder().Build();

        Should.Throw<ArgumentException>(() => sut.Credit(Rial(50), description!, "r"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Credit_WithNullOrWhitespaceReferenceId_ThrowsArgumentException(string? referenceId)
    {
        var sut = new WalletBuilder().Build();

        Should.Throw<ArgumentException>(() => sut.Credit(Rial(50), "d", referenceId!));
    }

    [Fact]
    public void Debit_OnActiveWalletWithSufficientBalance_DecreasesBalanceAndRaisesEvent()
    {
        var sut = BuildFunded(200);
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Debit(Rial(80), "purchase", "order-1");

        sut.Balance.Amount.ShouldBe(120m);
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.Single().ShouldBeOfType<WalletDebitedEvent>();
    }

    [Fact]
    public void Debit_OnInactiveWallet_ThrowsWalletInactiveException()
    {
        var sut = BuildFunded(200);
        sut.Freeze("audit", UserId.NewId());

        Should.Throw<WalletInactiveException>(() => sut.Debit(Rial(50), "d", "r"));
    }

    [Fact]
    public void Debit_WithInsufficientAvailableBalance_ThrowsInsufficientWalletBalanceException()
    {
        var sut = BuildFunded(30);

        Should.Throw<InsufficientWalletBalanceException>(() => sut.Debit(Rial(100), "d", "r"));
    }

    [Fact]
    public void Debit_WithZeroAmount_ThrowsInvalidWalletAmountException()
    {
        var sut = BuildFunded(100);

        Should.Throw<InvalidWalletAmountException>(() => sut.Debit(Rial(0), "d", "r"));
    }

    [Fact]
    public void Debit_RespectsReservedBalance()
    {
        var sut = BuildFunded(200);
        sut.CreateReservation(WalletReservationId.NewId(), Rial(150), "hold");

        Should.Throw<InsufficientWalletBalanceException>(() => sut.Debit(Rial(100), "d", "r"));
        Should.NotThrow(() => sut.Debit(Rial(50), "d", "r2"));
    }

    [Fact]
    public void CreateReservation_ReducesAvailableBalanceButNotBalance()
    {
        var sut = BuildFunded(500);

        sut.CreateReservation(WalletReservationId.NewId(), Rial(200), "hold-1");

        sut.Balance.Amount.ShouldBe(500m);
        sut.ReservedBalance.Amount.ShouldBe(200m);
        sut.AvailableBalance.Amount.ShouldBe(300m);
    }

    [Fact]
    public void CreateReservation_OnInactiveWallet_ThrowsWalletInactiveException()
    {
        var sut = BuildFunded(500);
        sut.Freeze("audit", UserId.NewId());

        Should.Throw<WalletInactiveException>(() =>
            sut.CreateReservation(WalletReservationId.NewId(), Rial(50), "hold"));
    }

    [Fact]
    public void CreateReservation_WithInsufficientAvailable_ThrowsInsufficientWalletBalanceException()
    {
        var sut = BuildFunded(50);

        Should.Throw<InsufficientWalletBalanceException>(() =>
            sut.CreateReservation(WalletReservationId.NewId(), Rial(100), "hold"));
    }

    [Fact]
    public void CreateReservation_RaisesWalletReservationCreatedEventAndReturnsActiveReservation()
    {
        var sut = BuildFunded(500);
        sut.ClearDomainEvents();
        var reservationId = WalletReservationId.NewId();

        var reservation = sut.CreateReservation(reservationId, Rial(100), "hold");

        reservation.Id.ShouldBe(reservationId);
        reservation.Status.ShouldBe(WalletReservationStatus.Active);
        reservation.Amount.Amount.ShouldBe(100m);
        sut.ActiveReservations.ShouldContain(reservation);
        sut.DomainEvents.Single().ShouldBeOfType<WalletReservationCreatedEvent>();
    }

    [Fact]
    public void ReleaseReservation_WithExistingId_RestoresAvailableBalanceAndRaisesEvent()
    {
        var sut = BuildFunded(500);
        var reservationId = WalletReservationId.NewId();
        sut.CreateReservation(reservationId, Rial(100), "hold");
        sut.ClearDomainEvents();

        sut.ReleaseReservation(reservationId);

        sut.ReservedBalance.Amount.ShouldBe(0m);
        sut.AvailableBalance.Amount.ShouldBe(500m);
        sut.ActiveReservations.ShouldBeEmpty();
        sut.DomainEvents.Single().ShouldBeOfType<WalletReservationReleasedEvent>();
    }

    [Fact]
    public void ReleaseReservation_WithUnknownId_IsSilentNoOp()
    {
        var sut = BuildFunded(500);
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        Should.NotThrow(() => sut.ReleaseReservation(WalletReservationId.NewId()));

        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ReleaseReservation_CalledTwiceOnSameId_SecondCallIsSilentNoOp()
    {
        var sut = BuildFunded(500);
        var reservationId = WalletReservationId.NewId();
        sut.CreateReservation(reservationId, Rial(100), "hold");
        sut.ReleaseReservation(reservationId);
        sut.ClearDomainEvents();

        Should.NotThrow(() => sut.ReleaseReservation(reservationId));
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void CreateDebitRequest_OnActiveWalletWithSufficientBalance_ReservesFundsAndRaisesEvent()
    {
        var sut = BuildFunded(500);
        sut.ClearDomainEvents();
        var requestId = WalletDebitRequestId.NewId();

        var request = sut.CreateDebitRequest(
            requestId, Rial(100), "reason", "desc", UserId.NewId(), TimeSpan.FromHours(1));

        request.Id.ShouldBe(requestId);
        request.Status.ShouldBe(WalletDebitRequestStatus.Pending);
        request.Amount.Amount.ShouldBe(100m);
        sut.DebitRequests.ShouldContain(request);
        sut.ReservedBalance.Amount.ShouldBe(100m);
        sut.AvailableBalance.Amount.ShouldBe(400m);
        sut.Balance.Amount.ShouldBe(500m);
        sut.DomainEvents.Single().ShouldBeOfType<WalletDebitRequestCreatedEvent>();
    }

    [Fact]
    public void CreateDebitRequest_OnInactiveWallet_ThrowsWalletInactiveException()
    {
        var sut = BuildFunded(500);
        sut.Freeze("audit", UserId.NewId());

        Should.Throw<WalletInactiveException>(() =>
            sut.CreateDebitRequest(WalletDebitRequestId.NewId(), Rial(10), "r", null, UserId.NewId(), TimeSpan.FromHours(1)));
    }

    [Fact]
    public void CreateDebitRequest_WithInsufficientAvailable_ThrowsInsufficientWalletBalanceException()
    {
        var sut = BuildFunded(50);

        Should.Throw<InsufficientWalletBalanceException>(() =>
            sut.CreateDebitRequest(WalletDebitRequestId.NewId(), Rial(100), "r", null, UserId.NewId(), TimeSpan.FromHours(1)));
    }

    [Fact]
    public void ApproveDebitRequest_ByOwner_ReleasesReservationDebitsBalanceAndRaisesThreeEvents()
    {
        var ownerId = UserId.NewId();
        var sut = new WalletBuilder().WithOwnerId(ownerId).Build();
        sut.Credit(Rial(500), "seed", "seed");
        var requestId = WalletDebitRequestId.NewId();
        sut.CreateDebitRequest(requestId, Rial(100), "reason", null, UserId.NewId(), TimeSpan.FromHours(1));
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.ApproveDebitRequest(requestId, ownerId);

        sut.Balance.Amount.ShouldBe(400m);
        sut.ReservedBalance.Amount.ShouldBe(0m);
        sut.AvailableBalance.Amount.ShouldBe(400m);
        sut.DebitRequests.Single().Status.ShouldBe(WalletDebitRequestStatus.Approved);
        sut.Version.ShouldBe(versionBefore + 3);
        sut.DomainEvents.Count.ShouldBe(3);
        sut.DomainEvents.ElementAt(0).ShouldBeOfType<WalletReservationReleasedEvent>();
        sut.DomainEvents.ElementAt(1).ShouldBeOfType<WalletDebitedEvent>();
        sut.DomainEvents.ElementAt(2).ShouldBeOfType<WalletDebitRequestApprovedEvent>();
    }

    [Fact]
    public void ApproveDebitRequest_ByNonOwner_ThrowsUnauthorizedWalletDebitApprovalException()
    {
        var ownerId = UserId.NewId();
        var sut = new WalletBuilder().WithOwnerId(ownerId).Build();
        sut.Credit(Rial(500), "seed", "seed");
        var requestId = WalletDebitRequestId.NewId();
        sut.CreateDebitRequest(requestId, Rial(100), "reason", null, UserId.NewId(), TimeSpan.FromHours(1));

        Should.Throw<UnauthorizedWalletDebitApprovalException>(() =>
            sut.ApproveDebitRequest(requestId, UserId.NewId()));
    }

    [Fact]
    public void ApproveDebitRequest_WithUnknownRequestId_ThrowsWalletDebitRequestNotFoundException()
    {
        var ownerId = UserId.NewId();
        var sut = new WalletBuilder().WithOwnerId(ownerId).Build();

        Should.Throw<WalletDebitRequestNotFoundException>(() =>
            sut.ApproveDebitRequest(WalletDebitRequestId.NewId(), ownerId));
    }

    [Fact]
    public void ApproveDebitRequest_AlreadyApproved_ThrowsInvalidWalletDebitRequestStatusException()
    {
        var ownerId = UserId.NewId();
        var sut = new WalletBuilder().WithOwnerId(ownerId).Build();
        sut.Credit(Rial(500), "seed", "seed");
        var requestId = WalletDebitRequestId.NewId();
        sut.CreateDebitRequest(requestId, Rial(100), "reason", null, UserId.NewId(), TimeSpan.FromHours(1));
        sut.ApproveDebitRequest(requestId, ownerId);

        Should.Throw<InvalidWalletDebitRequestStatusException>(() => sut.ApproveDebitRequest(requestId, ownerId));
    }

    [Fact]
    public void ApproveDebitRequest_OnExpiredRequest_MarksExpiredReleasesReservationAndThrows()
    {
        var ownerId = UserId.NewId();
        var sut = new WalletBuilder().WithOwnerId(ownerId).Build();
        sut.Credit(Rial(500), "seed", "seed");
        var requestId = WalletDebitRequestId.NewId();
        sut.CreateDebitRequest(requestId, Rial(100), "reason", null, UserId.NewId(), TimeSpan.FromSeconds(-1));
        sut.ClearDomainEvents();

        Should.Throw<WalletDebitRequestExpiredException>(() => sut.ApproveDebitRequest(requestId, ownerId));

        sut.DebitRequests.Single().Status.ShouldBe(WalletDebitRequestStatus.Expired);
        sut.ReservedBalance.Amount.ShouldBe(0m);
        sut.Balance.Amount.ShouldBe(500m);
        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<WalletReservationReleasedEvent>();
    }

    [Fact]
    public void RejectDebitRequest_ByOwner_ReleasesReservationMarksRejectedAndRaisesTwoEvents()
    {
        var ownerId = UserId.NewId();
        var sut = new WalletBuilder().WithOwnerId(ownerId).Build();
        sut.Credit(Rial(500), "seed", "seed");
        var requestId = WalletDebitRequestId.NewId();
        sut.CreateDebitRequest(requestId, Rial(100), "reason", null, UserId.NewId(), TimeSpan.FromHours(1));
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.RejectDebitRequest(requestId, ownerId, "not authorized");

        sut.Balance.Amount.ShouldBe(500m);
        sut.ReservedBalance.Amount.ShouldBe(0m);
        sut.DebitRequests.Single().Status.ShouldBe(WalletDebitRequestStatus.Rejected);
        sut.DebitRequests.Single().RejectionReason.ShouldBe("not authorized");
        sut.Version.ShouldBe(versionBefore + 2);
        sut.DomainEvents.Count.ShouldBe(2);
        sut.DomainEvents.ElementAt(0).ShouldBeOfType<WalletReservationReleasedEvent>();
        sut.DomainEvents.ElementAt(1).ShouldBeOfType<WalletDebitRequestRejectedEvent>();
    }

    [Fact]
    public void RejectDebitRequest_ByNonOwner_ThrowsUnauthorizedWalletDebitApprovalException()
    {
        var ownerId = UserId.NewId();
        var sut = new WalletBuilder().WithOwnerId(ownerId).Build();
        sut.Credit(Rial(500), "seed", "seed");
        var requestId = WalletDebitRequestId.NewId();
        sut.CreateDebitRequest(requestId, Rial(100), "reason", null, UserId.NewId(), TimeSpan.FromHours(1));

        Should.Throw<UnauthorizedWalletDebitApprovalException>(() =>
            sut.RejectDebitRequest(requestId, UserId.NewId(), "no"));
    }

    [Fact]
    public void CancelDebitRequest_ByAnyCaller_ReleasesReservationMarksCancelledAndRaisesTwoEvents()
    {
        var ownerId = UserId.NewId();
        var sut = new WalletBuilder().WithOwnerId(ownerId).Build();
        sut.Credit(Rial(500), "seed", "seed");
        var requestId = WalletDebitRequestId.NewId();
        sut.CreateDebitRequest(requestId, Rial(100), "reason", null, UserId.NewId(), TimeSpan.FromHours(1));
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.CancelDebitRequest(requestId, UserId.NewId());

        sut.Balance.Amount.ShouldBe(500m);
        sut.ReservedBalance.Amount.ShouldBe(0m);
        sut.DebitRequests.Single().Status.ShouldBe(WalletDebitRequestStatus.Cancelled);
        sut.Version.ShouldBe(versionBefore + 2);
        sut.DomainEvents.Count.ShouldBe(2);
        sut.DomainEvents.ElementAt(0).ShouldBeOfType<WalletReservationReleasedEvent>();
        sut.DomainEvents.ElementAt(1).ShouldBeOfType<WalletDebitRequestCancelledEvent>();
    }

    [Fact]
    public void CancelDebitRequest_WithUnknownId_ThrowsWalletDebitRequestNotFoundException()
    {
        var sut = new WalletBuilder().Build();

        Should.Throw<WalletDebitRequestNotFoundException>(() =>
            sut.CancelDebitRequest(WalletDebitRequestId.NewId(), UserId.NewId()));
    }

    [Fact]
    public void CancelDebitRequest_AlreadyResolved_ThrowsInvalidWalletDebitRequestStatusException()
    {
        var ownerId = UserId.NewId();
        var sut = new WalletBuilder().WithOwnerId(ownerId).Build();
        sut.Credit(Rial(500), "seed", "seed");
        var requestId = WalletDebitRequestId.NewId();
        sut.CreateDebitRequest(requestId, Rial(100), "reason", null, UserId.NewId(), TimeSpan.FromHours(1));
        sut.CancelDebitRequest(requestId, UserId.NewId());

        Should.Throw<InvalidWalletDebitRequestStatusException>(() =>
            sut.CancelDebitRequest(requestId, UserId.NewId()));
    }

    [Fact]
    public void Freeze_OnActiveWallet_SetsFreezeStateAndRaisesEvent()
    {
        var sut = new WalletBuilder().Build();
        sut.ClearDomainEvents();
        var adminId = UserId.NewId();

        sut.Freeze("suspicious", adminId);

        sut.IsActive.ShouldBeFalse();
        sut.FreezeReason.ShouldBe("suspicious");
        sut.FrozenAt.ShouldNotBeNull();
        sut.FrozenBy.ShouldBe(adminId);
        sut.DomainEvents.Single().ShouldBeOfType<WalletFrozenEvent>();
    }

    [Fact]
    public void Freeze_WhenAlreadyFrozen_IsNoOp()
    {
        var sut = new WalletBuilder().Build();
        sut.Freeze("first", UserId.NewId());
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;
        var reasonBefore = sut.FreezeReason;

        sut.Freeze("second", UserId.NewId());

        sut.FreezeReason.ShouldBe(reasonBefore);
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Freeze_WithNullOrWhitespaceReason_ThrowsArgumentException(string? reason)
    {
        var sut = new WalletBuilder().Build();

        Should.Throw<ArgumentException>(() => sut.Freeze(reason!, UserId.NewId()));
    }

    [Fact]
    public void Freeze_WithNullAdminId_ThrowsArgumentException()
    {
        var sut = new WalletBuilder().Build();

        Should.Throw<ArgumentException>(() => sut.Freeze("reason", null!));
    }

    [Fact]
    public void Unfreeze_OnFrozenWallet_RestoresActiveAndClearsFreezeStateAndRaisesEvent()
    {
        var sut = new WalletBuilder().Build();
        sut.Freeze("audit", UserId.NewId());
        sut.ClearDomainEvents();

        sut.Unfreeze(UserId.NewId(), "cleared");

        sut.IsActive.ShouldBeTrue();
        sut.FreezeReason.ShouldBeNull();
        sut.FrozenAt.ShouldBeNull();
        sut.FrozenBy.ShouldBeNull();
        sut.DomainEvents.Single().ShouldBeOfType<WalletUnfrozenEvent>();
    }

    [Fact]
    public void Unfreeze_WhenAlreadyActive_IsNoOp()
    {
        var sut = new WalletBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Unfreeze(UserId.NewId(), "reason");

        sut.IsActive.ShouldBeTrue();
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ReservedBalance_SumsOnlyActiveReservations()
    {
        var sut = BuildFunded(500);
        sut.CreateReservation(WalletReservationId.NewId(), Rial(100), "hold-1");
        var second = WalletReservationId.NewId();
        sut.CreateReservation(second, Rial(150), "hold-2");
        sut.CreateReservation(WalletReservationId.NewId(), Rial(50), "hold-3");
        sut.ReleaseReservation(second);

        sut.ReservedBalance.Amount.ShouldBe(150m);
        sut.AvailableBalance.Amount.ShouldBe(350m);
    }

    [Fact]
    public void LifecycleSequence_CreditReserveDebitFreeze_ProducesConsistentBalances()
    {
        var sut = new WalletBuilder().Build();

        sut.Credit(Rial(1000), "seed", "seed");
        sut.CreateReservation(WalletReservationId.NewId(), Rial(300), "hold");
        sut.Debit(Rial(200), "purchase", "order");
        sut.Freeze("audit", UserId.NewId());

        sut.Balance.Amount.ShouldBe(800m);
        sut.ReservedBalance.Amount.ShouldBe(300m);
        sut.AvailableBalance.Amount.ShouldBe(500m);
        sut.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void LifecycleSequence_VersionGrowsByEventCount()
    {
        var sut = new WalletBuilder().Build();

        sut.Version.ShouldBe(1);
        sut.Credit(Rial(100), "d", "r");
        sut.Version.ShouldBe(2);
        sut.CreateReservation(WalletReservationId.NewId(), Rial(30), "h");
        sut.Version.ShouldBe(3);
        sut.Debit(Rial(50), "d", "r");
        sut.Version.ShouldBe(4);
        sut.Freeze("reason", UserId.NewId());
        sut.Version.ShouldBe(5);
        sut.Unfreeze(UserId.NewId(), "clear");
        sut.Version.ShouldBe(6);
    }

    [Fact]
    public void Equality_TwoWalletsWithSameId_AreConsideredEqualByEntitySemantics()
    {
        var sut = new WalletBuilder().Build();

        sut.Equals(sut).ShouldBeTrue();
    }

    [Fact]
    public void Equality_TwoWalletsWithDifferentIds_AreConsideredUnequal()
    {
        var a = new WalletBuilder().Build();
        var b = new WalletBuilder().Build();

        a.Equals(b).ShouldBeFalse();
    }
}
