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

    public void Credit(Money amount, string description, string referenceId,
        string? idempotencyKey = null, string? correlationId = null)
    {
        ValidateAmount(amount);
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(referenceId, nameof(referenceId));

        Balance = Balance.Add(amount);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletCreditedEvent(
            Id, OwnerId, amount, Balance, description, referenceId, idempotencyKey, correlationId));
    }

    public void Debit(Money amount, string description, string referenceId,
        string? idempotencyKey = null, string? correlationId = null)
    {
        EnsureActive();
        ValidateAmount(amount);
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(referenceId, nameof(referenceId));

        if (AvailableBalance.IsLessThan(amount))
            throw new InsufficientWalletBalanceException(Id, amount, AvailableBalance);

        Balance = Balance.Subtract(amount);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletDebitedEvent(
            Id, OwnerId, amount, Balance, description, referenceId, idempotencyKey, correlationId));
    }

    public WalletDebitRequest CreateDebitRequest(WalletDebitRequestId requestId, Money amount,
        string reason, string? description, UserId requestedBy, TimeSpan expiryDuration)
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
            $"AdminDebitRequest:{requestId.Value}");
        _reservations.Add(reservation);

        var request = WalletDebitRequest.Create(
            requestId, Id, OwnerId, amount, reason, description,
            requestedBy, reservation.Id, DateTime.UtcNow.Add(expiryDuration));
        _debitRequests.Add(request);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletDebitRequestCreatedEvent(
            Id, OwnerId, requestId, amount, reason, requestedBy));

        return request;
    }

    public void ApproveDebitRequest(WalletDebitRequestId requestId, UserId approvedBy)
    {
        Guard.Against.Null(requestId, nameof(requestId));
        Guard.Against.Null(approvedBy, nameof(approvedBy));

        var request = _debitRequests.FirstOrDefault(r => r.Id == requestId)
            ?? throw new WalletDebitRequestNotFoundException(requestId);

        if (!approvedBy.Equals(OwnerId))
            throw new UnauthorizedWalletDebitApprovalException();

        if (request.Status != WalletDebitRequestStatus.Pending)
            throw new InvalidWalletDebitRequestStatusException(request.Status.ToString());

        if (request.ExpiresAt <= DateTime.UtcNow)
        {
            request.MarkExpired();
            ReleaseReservationInternal(request.ReservationId);
            UpdatedAt = DateTime.UtcNow;
            throw new WalletDebitRequestExpiredException();
        }

        ReleaseReservationInternal(request.ReservationId);

        Balance = Balance.Subtract(request.Amount);
        request.Approve(approvedBy);
        UpdatedAt = DateTime.UtcNow;

        var deterministicIdempotencyKey = $"debit-req-approve:{requestId.Value:N}";

        RaiseDomainEvent(new WalletDebitedEvent(
            Id, OwnerId, request.Amount, Balance,
            $"AdminDebit-Approved: {request.Reason}",
            requestId.Value.ToString(),
            deterministicIdempotencyKey));

        RaiseDomainEvent(new WalletDebitRequestApprovedEvent(
            Id, OwnerId, requestId, request.Amount, approvedBy));
    }

    public void RejectDebitRequest(WalletDebitRequestId requestId, UserId rejectedBy, string? rejectionReason)
    {
        Guard.Against.Null(requestId, nameof(requestId));
        Guard.Against.Null(rejectedBy, nameof(rejectedBy));

        var request = _debitRequests.FirstOrDefault(r => r.Id == requestId)
            ?? throw new WalletDebitRequestNotFoundException(requestId);

        if (!rejectedBy.Equals(OwnerId))
            throw new UnauthorizedWalletDebitApprovalException();

        if (request.Status != WalletDebitRequestStatus.Pending)
            throw new InvalidWalletDebitRequestStatusException(request.Status.ToString());

        ReleaseReservationInternal(request.ReservationId);
        request.Reject(rejectedBy, rejectionReason);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletDebitRequestRejectedEvent(
            Id, OwnerId, requestId, request.Amount, rejectedBy, rejectionReason));
    }

    public void CancelDebitRequest(WalletDebitRequestId requestId, UserId cancelledBy)
    {
        Guard.Against.Null(requestId, nameof(requestId));
        Guard.Against.Null(cancelledBy, nameof(cancelledBy));

        var request = _debitRequests.FirstOrDefault(r => r.Id == requestId)
            ?? throw new WalletDebitRequestNotFoundException(requestId);

        if (request.Status != WalletDebitRequestStatus.Pending)
            throw new InvalidWalletDebitRequestStatusException(request.Status.ToString());

        ReleaseReservationInternal(request.ReservationId);
        request.Cancel(cancelledBy);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletDebitRequestCancelledEvent(
            Id, OwnerId, requestId, request.Amount, cancelledBy));
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
        _reservations.Add(reservation);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletReservationCreatedEvent(Id, OwnerId, reservationId, amount, purpose));
        return reservation;
    }

    public void ReleaseReservation(WalletReservationId reservationId)
    {
        Guard.Against.Null(reservationId, nameof(reservationId));
        ReleaseReservationInternal(reservationId);
        UpdatedAt = DateTime.UtcNow;
    }

    private void ReleaseReservationInternal(WalletReservationId reservationId)
    {
        var reservation = _reservations.FirstOrDefault(r =>
            r.Id == reservationId && r.Status == WalletReservationStatus.Active);
        if (reservation is null)
            return;

        reservation.Release();

        RaiseDomainEvent(new WalletReservationReleasedEvent(Id, OwnerId, reservationId, reservation.Amount));
    }

    public void Freeze(string reason, UserId adminId)
    {
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));
        Guard.Against.Null(adminId, nameof(adminId));

        if (!IsActive) return;

        IsActive = false;
        FreezeReason = reason;
        FrozenAt = DateTime.UtcNow;
        FrozenBy = adminId;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletFrozenEvent(Id, OwnerId, reason, adminId));
    }

    public void Unfreeze(UserId adminId, string reason)
    {
        Guard.Against.Null(adminId, nameof(adminId));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        if (IsActive) return;

        IsActive = true;
        FreezeReason = null;
        FrozenAt = null;
        FrozenBy = null;
        UpdatedAt = DateTime.UtcNow;

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
