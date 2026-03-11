using Domain.Wallet.Entities;
using Domain.Wallet.Exceptions;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Aggregates;

public sealed class Wallet : AggregateRoot<WalletId>
{
    private readonly List<WalletReservation> _activeReservations = new();

    private Wallet()
    { }

    public UserId OwnerId { get; private set; } = default!;
    public Money Balance { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<WalletReservation> ActiveReservations => _activeReservations.AsReadOnly();

    public Money ReservedBalance => Money.Create(
        _activeReservations
            .Where(r => r.Status == WalletReservationStatus.Active)
            .Sum(r => r.Amount.Amount),
        Balance.Currency);

    public Money AvailableBalance => Balance - ReservedBalance;

    public static Wallet Create(WalletId id, UserId ownerId, string currency)
    {
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
        if (!IsActive)
            throw new WalletInactiveException(Id);

        if (amount.Amount <= 0)
            throw new InvalidWalletAmountException(amount.Amount);

        Balance = Balance + amount;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletCreditedEvent(Id, OwnerId, amount, Balance, description, referenceId));
    }

    public void Debit(Money amount, string description, string referenceId)
    {
        if (!IsActive)
            throw new WalletInactiveException(Id);

        if (amount.Amount <= 0)
            throw new InvalidWalletAmountException(amount.Amount);

        if (AvailableBalance < amount)
            throw new InsufficientWalletBalanceException(Id, amount, AvailableBalance);

        Balance = Balance - amount;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletDebitedEvent(Id, OwnerId, amount, Balance, description, referenceId));
    }

    public WalletReservation CreateReservation(WalletReservationId reservationId, Money amount, string purpose)
    {
        if (!IsActive)
            throw new WalletInactiveException(Id);

        if (AvailableBalance < amount)
            throw new InsufficientWalletBalanceException(Id, amount, AvailableBalance);

        var reservation = WalletReservation.Create(reservationId, Id, amount, purpose);
        _activeReservations.Add(reservation);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletReservationCreatedEvent(Id, OwnerId, reservationId, amount, purpose));
        return reservation;
    }

    public void ConfirmReservation(WalletReservationId reservationId, string description, string referenceId)
    {
        var reservation = _activeReservations.FirstOrDefault(r => r.Id == reservationId)
            ?? throw new WalletReservationNotFoundException(reservationId);

        reservation.Confirm();
        Balance = Balance - reservation.Amount;
        _activeReservations.Remove(reservation);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletReservationConfirmedEvent(Id, OwnerId, reservationId, reservation.Amount, description, referenceId));
    }

    public void ReleaseReservation(WalletReservationId reservationId)
    {
        var reservation = _activeReservations.FirstOrDefault(r => r.Id == reservationId)
            ?? throw new WalletReservationNotFoundException(reservationId);

        reservation.Release();
        _activeReservations.Remove(reservation);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletReservationReleasedEvent(Id, OwnerId, reservationId, reservation.Amount));
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}