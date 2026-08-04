using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;
using SharedKernel.Localization;

namespace Domain.Wallet.Aggregates;

public sealed class WalletWithdrawalRequest : AggregateRoot<WalletWithdrawalRequestId>
{
    private const decimal MinimumAmount = 50_000m;

    public UserId UserId { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;
    public IbanNumber Iban { get; private set; } = default!;
    public string AccountHolder { get; private set; } = default!;
    public string? Description { get; private set; }
    public WalletReservationId ReservationId { get; private set; } = default!;
    public WalletWithdrawalStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? BankReferenceNumber { get; private set; }
    public UserId? ProcessedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private WalletWithdrawalRequest()
    { }

    public static WalletWithdrawalRequest Create(
        UserId userId,
        Money amount,
        IbanNumber iban,
        string accountHolder,
        WalletReservationId reservationId,
        string? description = null)
    {
        if (userId is null) throw new DomainException(DomainErrorCodes.Wallet.WithdrawalUserIdRequired, "UserId is required.");
        if (amount is null) throw new DomainException(DomainErrorCodes.Wallet.WithdrawalAmountRequired, "Amount is required.");
        if (iban is null) throw new DomainException(DomainErrorCodes.Wallet.WithdrawalIbanRequired, "IBAN is required.");
        if (string.IsNullOrWhiteSpace(accountHolder))
            throw new DomainException(DomainErrorCodes.Wallet.WithdrawalAccountHolderRequired, "Account holder is required.");
        if (reservationId is null)
            throw new DomainException(DomainErrorCodes.Wallet.WithdrawalReservationIdRequired, "Reservation id is required.");

        if (amount.Amount < MinimumAmount)
            throw new DomainException(
                DomainErrorCodes.Wallet.WithdrawalMinimumAmount,
                $"Minimum withdrawal amount is {MinimumAmount:N0}.",
                new Dictionary<string, object?> { ["minimum"] = MinimumAmount });

        var request = new WalletWithdrawalRequest
        {
            Id = WalletWithdrawalRequestId.NewId(),
            UserId = userId,
            Amount = amount,
            Iban = iban,
            AccountHolder = accountHolder.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ReservationId = reservationId,
            Status = WalletWithdrawalStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        request.RaiseDomainEvent(new WithdrawalRequestedEvent(request.Id, userId, amount, reservationId));
        return request;
    }

    public void Approve(UserId adminId)
    {
        EnsureCanTransition("approve");
        Status = WalletWithdrawalStatus.Approved;
        ProcessedBy = adminId;
        ApprovedAt = DateTime.UtcNow;
        RaiseDomainEvent(new WithdrawalApprovedEvent(Id, UserId, adminId));
    }

    public void Reject(UserId adminId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException(
                DomainErrorCodes.Wallet.WithdrawalRejectionReasonRequired,
                "Rejection reason is required.");

        EnsureCanTransition("reject");
        Status = WalletWithdrawalStatus.Rejected;
        RejectionReason = reason.Trim();
        ProcessedBy = adminId;
        RejectedAt = DateTime.UtcNow;
        RaiseDomainEvent(new WithdrawalRejectedEvent(Id, UserId, adminId, RejectionReason));
    }

    public void MarkPaid(UserId adminId, string bankReferenceNumber)
    {
        if (Status != WalletWithdrawalStatus.Approved && Status != WalletWithdrawalStatus.Pending)
            throw new DomainException(
                DomainErrorCodes.Wallet.WithdrawalInvalidStateForPay,
                $"Withdrawal in status '{Status}' cannot be marked paid.",
                new Dictionary<string, object?> { ["status"] = Status.ToString() });

        if (string.IsNullOrWhiteSpace(bankReferenceNumber))
            throw new DomainException(
                DomainErrorCodes.Wallet.WithdrawalBankReferenceRequired,
                "Bank reference number is required.");

        Status = WalletWithdrawalStatus.Paid;
        ProcessedBy = adminId;
        BankReferenceNumber = bankReferenceNumber.Trim();
        PaidAt = DateTime.UtcNow;
        RaiseDomainEvent(new WithdrawalPaidEvent(Id, UserId, Amount, adminId, BankReferenceNumber));
    }

    public void Cancel(UserId requester)
    {
        if (!UserId.Equals(requester))
            throw new DomainException(
                DomainErrorCodes.Wallet.WithdrawalOnlyOwnerCanCancel,
                "Only the request owner can cancel it.");

        EnsureCanTransition("cancel");
        Status = WalletWithdrawalStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        RaiseDomainEvent(new WithdrawalCancelledEvent(Id, UserId));
    }

    private void EnsureCanTransition(string action)
    {
        if (Status != WalletWithdrawalStatus.Pending)
            throw new DomainException(
                DomainErrorCodes.Wallet.WithdrawalInvalidStateForAction,
                $"Request in status '{Status}' cannot perform action '{action}'.",
                new Dictionary<string, object?>
                {
                    ["status"] = Status.ToString(),
                    ["action"] = action
                });
    }
}
