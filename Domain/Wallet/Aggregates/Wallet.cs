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
    { }

    public Money Balance { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public string? FreezeReason { get; private set; }
    public DateTime? FrozenAt { get; private set; }
    public UserId? FrozenBy { get; private set; }

    public User.Aggregates.User Owner { get; private set; } = default!;
    public UserId OwnerId { get; private set; } = default!;

    private readonly List<WalletReservation> _reservations = [];
    public IReadOnlyList<WalletReservation> Reservations => _reservations;

    public IReadOnlyList<WalletReservation> ActiveReservations =>
        _reservations
            .Where(r => r.Status == WalletReservationStatus.Active)
            .ToList();

    private readonly List<WalletDebitRequest> _debitRequests = [];
    public IReadOnlyList<WalletDebitRequest> DebitRequests => _debitRequests;

    public Money ReservedBalance => Money.Create(
        _reservations
            .Where(r => r.Status == WalletReservationStatus.Active)
            .Sum(r => r.Amount.Amount),
        Balance.Currency);

    public Money AvailableBalance => Balance.Subtract(ReservedBalance);

    public static Wallet Create(UserId ownerId, DateTime now, string currency = "IRT")
    {
        Guard.Against.Null(ownerId, nameof(ownerId));
        Guard.Against.NullOrWhiteSpace(currency, nameof(currency));

        var wallet = new Wallet
        {
            Id = WalletId.NewId(),
            OwnerId = ownerId,
            Balance = Money.Zero(currency),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        wallet.RaiseDomainEvent(new WalletCreatedEvent(wallet.Id, ownerId, currency));
        return wallet;
    }

    public void Credit(Money amount, string description, string referenceId,
        DateTime now, string? idempotencyKey = null, string? correlationId = null)
    {
        ValidateAmount(amount);
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(referenceId, nameof(referenceId));

        Balance = Balance.Add(amount);
        UpdatedAt = now;

        RaiseDomainEvent(new WalletCreditedEvent(
            Id, OwnerId, amount, Balance, description, referenceId, idempotencyKey, correlationId));
    }

    public void Debit(Money amount, string description, string referenceId,
        DateTime now, string? idempotencyKey = null, string? correlationId = null)
    {
        EnsureActive();
        ValidateAmount(amount);
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(referenceId, nameof(referenceId));

        if (AvailableBalance.IsLessThan(amount))
            throw new InsufficientWalletBalanceException(Id, amount, AvailableBalance);

        Balance = Balance.Subtract(amount);
        UpdatedAt = now;

        RaiseDomainEvent(new WalletDebitedEvent(
            Id, OwnerId, amount, Balance, description, referenceId, idempotencyKey, correlationId));
    }

    public WalletDebitRequest CreateDebitRequest(WalletDebitRequestId requestId, Money amount,
        string reason, string? description, UserId requestedBy, TimeSpan expiryDuration, DateTime now)
    {
        EnsureActive();
        Guard.Against.Null(requestId, nameof(requestId));
        ValidateAmount(amount);
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
        Guard.Against.Null(requestedBy, nameof(requestedBy));

        if (AvailableBalance.IsLessThan(amount))
            throw new InsufficientWalletBalanceException(Id, amount, AvailableBalance);

        var reservation = WalletReservation.Create(
            WalletReservationId.NewId(),
            Id,
            amount,
            $"AdminDebitRequest:{requestId.Value}",
            now,
            now.Add(expiryDuration));
        _reservations.Add(reservation);

        var request = WalletDebitRequest.Create(
            requestId, Id, OwnerId, amount, reason, description,
            requestedBy, reservation.Id, now.Add(expiryDuration), now);
        _debitRequests.Add(request);
        UpdatedAt = now;

        RaiseDomainEvent(new WalletDebitRequestCreatedEvent(
            Id, OwnerId, requestId, amount, reason, requestedBy));

        return request;
    }

    public void ApproveDebitRequest(WalletDebitRequestId requestId, UserId approvedBy, DateTime now)
    {
        Guard.Against.Null(requestId, nameof(requestId));
        Guard.Against.Null(approvedBy, nameof(approvedBy));

        var request = _debitRequests.FirstOrDefault(r => r.Id == requestId)
            ?? throw new WalletDebitRequestNotFoundException(requestId);

        if (!approvedBy.Equals(OwnerId))
            throw new UnauthorizedWalletDebitApprovalException();

        if (request.Status != WalletDebitRequestStatus.Pending)
            throw new InvalidWalletDebitRequestStatusException(request.Status.ToString());

        if (request.ExpiresAt <= now)
        {
            request.MarkExpired(now);
            ReleaseReservationInternal(request.ReservationId, now);
            UpdatedAt = now;
            throw new WalletDebitRequestExpiredException();
        }

        ReleaseReservationInternal(request.ReservationId, now);

        Balance = Balance.Subtract(request.Amount);
        request.Approve(approvedBy, now);
        UpdatedAt = now;

        var deterministicIdempotencyKey = $"debit-req-approve:{requestId.Value:N}";

        RaiseDomainEvent(new WalletDebitedEvent(
            Id, OwnerId, request.Amount, Balance,
            $"AdminDebit-Approved: {request.Reason}",
            requestId.Value.ToString(),
            deterministicIdempotencyKey));

        RaiseDomainEvent(new WalletDebitRequestApprovedEvent(
            Id, OwnerId, requestId, request.Amount, approvedBy));
    }

    public void RejectDebitRequest(WalletDebitRequestId requestId, UserId rejectedBy, string? rejectionReason, DateTime now)
    {
        Guard.Against.Null(requestId, nameof(requestId));
        Guard.Against.Null(rejectedBy, nameof(rejectedBy));

        var request = _debitRequests.FirstOrDefault(r => r.Id == requestId)
            ?? throw new WalletDebitRequestNotFoundException(requestId);

        if (!rejectedBy.Equals(OwnerId))
            throw new UnauthorizedWalletDebitApprovalException();

        if (request.Status != WalletDebitRequestStatus.Pending)
            throw new InvalidWalletDebitRequestStatusException(request.Status.ToString());

        ReleaseReservationInternal(request.ReservationId, now);
        request.Reject(rejectedBy, rejectionReason, now);
        UpdatedAt = now;

        RaiseDomainEvent(new WalletDebitRequestRejectedEvent(
            Id, OwnerId, requestId, request.Amount, rejectedBy, rejectionReason));
    }

    public void CancelDebitRequest(WalletDebitRequestId requestId, UserId cancelledBy, DateTime now)
    {
        Guard.Against.Null(requestId, nameof(requestId));
        Guard.Against.Null(cancelledBy, nameof(cancelledBy));

        var request = _debitRequests.FirstOrDefault(r => r.Id == requestId)
            ?? throw new WalletDebitRequestNotFoundException(requestId);

        if (request.Status != WalletDebitRequestStatus.Pending)
            throw new InvalidWalletDebitRequestStatusException(request.Status.ToString());

        ReleaseReservationInternal(request.ReservationId, now);
        request.Cancel(cancelledBy, now);
        UpdatedAt = now;

        RaiseDomainEvent(new WalletDebitRequestCancelledEvent(
            Id, OwnerId, requestId, request.Amount, cancelledBy));
    }

    public WalletReservation CreateReservation(WalletReservationId reservationId, Money amount, string purpose, DateTime now, DateTime? expiresAt = null)
    {
        EnsureActive();
        Guard.Against.Null(reservationId, nameof(reservationId));
        ValidateAmount(amount);
        Guard.Against.NullOrWhiteSpace(purpose, nameof(purpose));

        if (AvailableBalance.IsLessThan(amount))
            throw new InsufficientWalletBalanceException(Id, amount, AvailableBalance);

        var reservation = WalletReservation.Create(reservationId, Id, amount, purpose, now, expiresAt);
        _reservations.Add(reservation);
        UpdatedAt = now;

        RaiseDomainEvent(new WalletReservationCreatedEvent(Id, OwnerId, reservationId, amount, purpose));
        return reservation;
    }

    public void ReleaseReservation(WalletReservationId reservationId, DateTime now)
    {
        Guard.Against.Null(reservationId, nameof(reservationId));
        ReleaseReservationInternal(reservationId, now);
        UpdatedAt = now;
    }

    private void ReleaseReservationInternal(WalletReservationId reservationId, DateTime now)
    {
        var reservation = _reservations.FirstOrDefault(r =>
            r.Id == reservationId && r.Status == WalletReservationStatus.Active);
        if (reservation is null)
            return;

        reservation.Release(now);

        RaiseDomainEvent(new WalletReservationReleasedEvent(Id, OwnerId, reservationId, reservation.Amount));
    }

    public void Freeze(string reason, UserId adminId, DateTime now)
    {
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
        Guard.Against.Null(adminId, nameof(adminId));

        if (!IsActive) return;

        IsActive = false;
        FreezeReason = reason;
        FrozenAt = now;
        FrozenBy = adminId;
        UpdatedAt = now;

        RaiseDomainEvent(new WalletFrozenEvent(Id, OwnerId, reason, adminId));
    }

    public void Unfreeze(UserId adminId, string reason, DateTime now)
    {
        Guard.Against.Null(adminId, nameof(adminId));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        if (IsActive) return;

        IsActive = true;
        FreezeReason = null;
        FrozenAt = null;
        FrozenBy = null;
        UpdatedAt = now;

        RaiseDomainEvent(new WalletUnfrozenEvent(Id, OwnerId, adminId, reason));
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
