using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Domain.Wallet.Entities;

public class WalletReservationTests
{
    private static Money Rial(decimal amount) => Money.Create(amount, "IRT");

    private static Wallets BuildFunded(decimal openingBalance)
    {
        var wallet = new WalletBuilder().Build();
        if (openingBalance > 0)
            wallet.Credit(Rial(openingBalance), "seed", "seed-ref");
        return wallet;
    }

    // ---------- Creation (via Wallet.CreateReservation) ----------

    [Fact]
    public void CreateReservation_WithValidInput_ReturnsActiveReservation()
    {
        var wallet = BuildFunded(500);
        var reservationId = WalletReservationId.NewId();
        var amount = Rial(100);

        var reservation = wallet.CreateReservation(reservationId, amount, "hold");

        reservation.Id.ShouldBe(reservationId);
        reservation.WalletId.ShouldBe(wallet.Id);
        reservation.Amount.ShouldBe(amount);
        reservation.Purpose.ShouldBe("hold");
        reservation.Status.ShouldBe(WalletReservationStatus.Active);
        reservation.ResolvedAt.ShouldBeNull();
    }

    [Fact]
    public void CreateReservation_SetsCreatedAtCloseToUtcNow()
    {
        var wallet = BuildFunded(500);
        var before = DateTime.UtcNow.AddSeconds(-1);

        var reservation = wallet.CreateReservation(WalletReservationId.NewId(), Rial(100), "hold");

        var after = DateTime.UtcNow.AddSeconds(1);
        reservation.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        reservation.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void CreateReservation_WithoutExpiration_LeavesExpiresAtNull()
    {
        var wallet = BuildFunded(500);

        var reservation = wallet.CreateReservation(WalletReservationId.NewId(), Rial(100), "hold");

        reservation.ExpiresAt.ShouldBeNull();
    }

    [Fact]
    public void CreateReservation_AddsReservationToActiveReservationsOfWallet()
    {
        var wallet = BuildFunded(500);
        var reservationId = WalletReservationId.NewId();

        var reservation = wallet.CreateReservation(reservationId, Rial(100), "hold");

        wallet.ActiveReservations.ShouldContain(reservation);
    }

    [Fact]
    public void CreateReservation_ReducesAvailableBalanceByReservationAmount()
    {
        var wallet = BuildFunded(500);

        wallet.CreateReservation(WalletReservationId.NewId(), Rial(150), "hold");

        wallet.Balance.Amount.ShouldBe(500m);
        wallet.ReservedBalance.Amount.ShouldBe(150m);
        wallet.AvailableBalance.Amount.ShouldBe(350m);
    }

    [Fact]
    public void CreateReservation_RaisesWalletReservationCreatedEvent()
    {
        var wallet = BuildFunded(500);
        wallet.ClearDomainEvents();
        var reservationId = WalletReservationId.NewId();
        var amount = Rial(100);

        wallet.CreateReservation(reservationId, amount, "hold");

        var evt = wallet.DomainEvents.Single().ShouldBeOfType<WalletReservationCreatedEvent>();
        evt.WalletId.ShouldBe(wallet.Id);
        evt.ReservationId.ShouldBe(reservationId);
        evt.Amount.ShouldBe(amount);
        evt.Purpose.ShouldBe("hold");
    }

    [Fact]
    public void CreateReservation_MultipleTimes_AccumulatesReservedBalance()
    {
        var wallet = BuildFunded(500);

        wallet.CreateReservation(WalletReservationId.NewId(), Rial(100), "hold-1");
        wallet.CreateReservation(WalletReservationId.NewId(), Rial(150), "hold-2");

        wallet.ReservedBalance.Amount.ShouldBe(250m);
        wallet.AvailableBalance.Amount.ShouldBe(250m);
        wallet.ActiveReservations.Count.ShouldBe(2);
    }

    // ---------- Release (via Wallet.ReleaseReservation) ----------

    [Fact]
    public void ReleaseReservation_OnActiveReservation_TransitionsToReleased()
    {
        var wallet = BuildFunded(500);
        var reservationId = WalletReservationId.NewId();
        var reservation = wallet.CreateReservation(reservationId, Rial(100), "hold");
        var before = DateTime.UtcNow.AddSeconds(-1);

        wallet.ReleaseReservation(reservationId);

        var after = DateTime.UtcNow.AddSeconds(1);
        reservation.Status.ShouldBe(WalletReservationStatus.Released);
        reservation.ResolvedAt.ShouldNotBeNull();
        reservation.ResolvedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        reservation.ResolvedAt.Value.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void ReleaseReservation_RemovesFromActiveReservationsAndRestoresAvailableBalance()
    {
        var wallet = BuildFunded(500);
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Rial(100), "hold");

        wallet.ReleaseReservation(reservationId);

        wallet.ActiveReservations.ShouldBeEmpty();
        wallet.ReservedBalance.Amount.ShouldBe(0m);
        wallet.AvailableBalance.Amount.ShouldBe(500m);
    }

    [Fact]
    public void ReleaseReservation_RaisesWalletReservationReleasedEvent()
    {
        var wallet = BuildFunded(500);
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Rial(100), "hold");
        wallet.ClearDomainEvents();

        wallet.ReleaseReservation(reservationId);

        var evt = wallet.DomainEvents.Single().ShouldBeOfType<WalletReservationReleasedEvent>();
        evt.WalletId.ShouldBe(wallet.Id);
        evt.ReservationId.ShouldBe(reservationId);
        evt.Amount.Amount.ShouldBe(100m);
    }

    [Fact]
    public void ReleaseReservation_WithUnknownId_IsSilentNoOp()
    {
        var wallet = BuildFunded(500);
        wallet.ClearDomainEvents();
        var versionBefore = wallet.Version;

        Should.NotThrow(() => wallet.ReleaseReservation(WalletReservationId.NewId()));

        wallet.Version.ShouldBe(versionBefore);
        wallet.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ReleaseReservation_CalledTwiceOnSameReservation_SecondCallIsSilentNoOp()
    {
        var wallet = BuildFunded(500);
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Rial(100), "hold");
        wallet.ReleaseReservation(reservationId);
        wallet.ClearDomainEvents();
        var versionBefore = wallet.Version;

        Should.NotThrow(() => wallet.ReleaseReservation(reservationId));

        wallet.Version.ShouldBe(versionBefore);
        wallet.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ReleaseReservation_OnlyReleasesTargetedReservationLeavingOthersActive()
    {
        var wallet = BuildFunded(500);
        var keepId = WalletReservationId.NewId();
        var releaseId = WalletReservationId.NewId();
        wallet.CreateReservation(keepId, Rial(100), "keep");
        var toRelease = wallet.CreateReservation(releaseId, Rial(150), "release");

        wallet.ReleaseReservation(releaseId);

        toRelease.Status.ShouldBe(WalletReservationStatus.Released);
        wallet.ActiveReservations.Count.ShouldBe(1);
        wallet.ActiveReservations.Single().Id.ShouldBe(keepId);
        wallet.ReservedBalance.Amount.ShouldBe(100m);
        wallet.AvailableBalance.Amount.ShouldBe(400m);
    }

    // ---------- Invariants at reservation-entity level (via the aggregate boundary) ----------

    [Fact]
    public void CreateReservation_WithInsufficientAvailableBalance_ThrowsDomainException()
    {
        var wallet = BuildFunded(50);

        Should.Throw<DomainException>(() =>
            wallet.CreateReservation(WalletReservationId.NewId(), Rial(100), "hold"));
    }

    [Fact]
    public void CreateReservation_OnInactiveWallet_ThrowsDomainException()
    {
        var wallet = BuildFunded(500);
        wallet.Freeze("audit", UserId.NewId());

        Should.Throw<DomainException>(() =>
            wallet.CreateReservation(WalletReservationId.NewId(), Rial(100), "hold"));
    }
}
