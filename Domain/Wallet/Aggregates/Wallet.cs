using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.Exceptions;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Aggregates;

public sealed class Wallet : AggregateRoot<WalletId>
{
    private Wallet()
    {
    }

    public Money Balance { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public User.Aggregates.User Owner { get; private set; } = default!;
    public UserId OwnerId { get; private set; } = default!;
    private readonly List<WalletReservation> _activeReservations = [];
    public IReadOnlyList<WalletReservation> ActiveReservations => _activeReservations.AsReadOnly();

    public Money ReservedBalance => Money.Create(
        _activeReservations
            .Where(r => r.Status == WalletReservationStatus.Active)
            .Sum(r => r.Amount.Amount),
        Balance.Currency);

    public Money AvailableBalance => Balance.Subtract(ReservedBalance);

    public static Wallet Create(WalletId id, UserId ownerId, string currency = "IRT")
    {
        Guard.Against.Null(id, nameof(id));
        Guard.Against.Null(ownerId, nameof(ownerId));
        Guard.Against.NullOrWhiteSpace(currency, nameof(currency));

        var wallet = new Wallet
        {
            Id = id,
            OwnerId = ownerId,
            Balance = Money.Zero(currency),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        wallet.RaiseDomainEvent(new WalletCreatedEvent(id, ownerId, currency));
        return wallet;
    }

    public void Credit(Money amount, string description, string referenceId)
    {
        EnsureActive();
        ValidateAmount(amount);
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(referenceId, nameof(referenceId));

        Balance = Balance.Add(amount);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletCreditedEvent(Id, OwnerId, amount, Balance, description, referenceId));
    }

    public void Debit(Money amount, string description, string referenceId)
    {
        EnsureActive();
        ValidateAmount(amount);
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(referenceId, nameof(referenceId));

        if (AvailableBalance.IsLessThan(amount))
            throw new InsufficientWalletBalanceException(Id, amount, AvailableBalance);

        Balance = Balance.Subtract(amount);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletDebitedEvent(Id, OwnerId, amount, Balance, description, referenceId));
    }

    public WalletReservation CreateReservation(WalletReservationId reservationId, Money amount, string purpose)
    {
        EnsureActive();
        Guard.Against.Null(reservationId, nameof(reservationId));
        ValidateAmount(amount);
        Guard.Against.NullOrWhiteSpace(purpose, nameof(purpose));

        if (AvailableBalance.IsLessThan(amount))
            throw new InsufficientWalletBalanceException(Id, amount, AvailableBalance);

        var reservation = WalletReservation.Create(reservationId, Id, amount, purpose);
        _activeReservations.Add(reservation);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletReservationCreatedEvent(Id, OwnerId, reservationId, amount, purpose));
        return reservation;
    }

    public void ConfirmReservation(WalletReservationId reservationId, string description, string referenceId)
    {
        Guard.Against.Null(reservationId, nameof(reservationId));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(referenceId, nameof(referenceId));

        var reservation = GetActiveReservation(reservationId);

        reservation.Confirm();
        Balance = Balance.Subtract(reservation.Amount);
        _activeReservations.Remove(reservation);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletReservationConfirmedEvent(Id, OwnerId, reservationId, reservation.Amount, description, referenceId));
    }

    public void ReleaseReservation(WalletReservationId reservationId)
    {
        Guard.Against.Null(reservationId, nameof(reservationId));

        var reservation = GetActiveReservation(reservationId);

        reservation.Release();
        _activeReservations.Remove(reservation);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletReservationReleasedEvent(Id, OwnerId, reservationId, reservation.Amount));
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletStatusChangedEvent(Id, OwnerId, WalletStatus.Active, null));
    }

    public void Deactivate(string? reason = null)
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletStatusChangedEvent(Id, OwnerId, WalletStatus.Suspended, reason));
    }

    public bool HasSufficientBalance(Money amount)
    {
        Guard.Against.Null(amount, nameof(amount));
        return AvailableBalance.IsGreaterThanOrEqual(amount);
    }

    public bool HasActiveReservation(WalletReservationId reservationId)
    {
        return _activeReservations.Any(r => r.Id == reservationId && r.Status == WalletReservationStatus.Active);
    }

    private WalletReservation GetActiveReservation(WalletReservationId reservationId)
    {
        var reservation = _activeReservations.FirstOrDefault(r => r.Id == reservationId);
        if (reservation is null)
            throw new WalletReservationNotFoundException(reservationId);
        return reservation;
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new WalletInactiveException(Id);
    }

    private static void ValidateAmount(Money amount)
    {
        Guard.Against.Null(amount, nameof(amount));
        if (amount.Amount <= 0)
            throw new InvalidWalletAmountException(amount.Amount);
    }
}