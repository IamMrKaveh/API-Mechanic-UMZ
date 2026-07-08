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

    public string? FreezeReason { get; private set; }
    public DateTime? FrozenAt { get; private set; }
    public UserId? FrozenBy { get; private set; }

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

    public static Wallet Create(UserId ownerId, string currency = "IRT")
    {
        Guard.Against.Null(ownerId, nameof(ownerId));
        Guard.Against.NullOrWhiteSpace(currency, nameof(currency));

        var wallet = new Wallet
        {
            Id = WalletId.NewId(),
            OwnerId = ownerId,
            Balance = Money.Zero(currency),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        wallet.RaiseDomainEvent(new WalletCreatedEvent(wallet.Id, ownerId, currency));
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

    public void ReleaseReservation(WalletReservationId reservationId)
    {
        Guard.Against.Null(reservationId, nameof(reservationId));

        var reservation = GetActiveReservation(reservationId);

        reservation.Release();
        _activeReservations.Remove(reservation);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletReservationReleasedEvent(Id, OwnerId, reservationId, reservation.Amount));
    }

    public void Freeze(string reason, UserId adminId)
    {
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
        Guard.Against.Null(adminId, nameof(adminId));

        if (!IsActive)
            throw new WalletInactiveException(Id);

        IsActive = false;
        FreezeReason = reason;
        FrozenAt = DateTime.UtcNow;
        FrozenBy = adminId;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletFrozenEvent(Id, OwnerId, reason, adminId));
    }

    public void Unfreeze(UserId adminId)
    {
        Guard.Against.Null(adminId, nameof(adminId));

        if (IsActive)
            return;

        IsActive = true;
        FreezeReason = null;
        FrozenAt = null;
        FrozenBy = null;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletUnfrozenEvent(Id, OwnerId, adminId));
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