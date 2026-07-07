using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;

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
    public WithdrawalStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public UserId? ApprovedBy { get; private set; }
    public UserId? RejectedBy { get; private set; }
    public UserId? PaidBy { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? BankReferenceNumber { get; private set; }

    private WalletWithdrawalRequest()
    {
    }

    public static WalletWithdrawalRequest Create(
        UserId userId,
        Money amount,
        IbanNumber iban,
        string accountHolder,
        WalletReservationId reservationId,
        string? description = null)
    {
        if (userId is null) throw new DomainException("شناسه کاربر الزامی است.");
        if (amount is null) throw new DomainException("مبلغ الزامی است.");
        if (iban is null) throw new DomainException("شماره شبا الزامی است.");
        if (string.IsNullOrWhiteSpace(accountHolder))
            throw new DomainException("نام صاحب حساب الزامی است.");
        if (reservationId is null) throw new DomainException("شناسه رزرو الزامی است.");

        if (amount.Amount < MinimumAmount)
            throw new DomainException($"حداقل مبلغ برداشت {MinimumAmount:N0} تومان است.");

        var withdrawal = new WalletWithdrawalRequest
        {
            Id = WalletWithdrawalRequestId.NewId(),
            UserId = userId,
            Amount = amount,
            Iban = iban,
            AccountHolder = accountHolder.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ReservationId = reservationId,
            Status = WithdrawalStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        withdrawal.RaiseDomainEvent(new WithdrawalRequestedEvent(
            withdrawal.Id, userId, amount, reservationId));

        return withdrawal;
    }

    public void Approve(UserId adminId)
    {
        EnsurePending("تأیید");
        Status = WithdrawalStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedBy = adminId;
        RaiseDomainEvent(new WithdrawalApprovedEvent(Id, UserId, adminId));
    }

    public void Reject(UserId adminId, string reason)
    {
        EnsurePending("رد");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("دلیل رد درخواست الزامی است.");

        Status = WithdrawalStatus.Rejected;
        RejectedAt = DateTime.UtcNow;
        RejectedBy = adminId;
        RejectionReason = reason.Trim();
        RaiseDomainEvent(new WithdrawalRejectedEvent(Id, UserId, adminId, RejectionReason));
    }

    public void MarkPaid(UserId adminId, string bankReferenceNumber)
    {
        if (Status is not WithdrawalStatus.Pending and not WithdrawalStatus.Approved)
            throw new DomainException($"درخواست برداشت در وضعیت '{Status}' قابل پرداخت نیست.");

        if (string.IsNullOrWhiteSpace(bankReferenceNumber))
            throw new DomainException("شماره پیگیری بانکی الزامی است.");

        Status = WithdrawalStatus.Paid;
        PaidAt = DateTime.UtcNow;
        PaidBy = adminId;
        BankReferenceNumber = bankReferenceNumber.Trim();

        if (ApprovedAt is null)
        {
            ApprovedAt = PaidAt;
            ApprovedBy = adminId;
        }

        RaiseDomainEvent(new WithdrawalPaidEvent(Id, UserId, Amount, adminId, BankReferenceNumber));
    }

    public void Cancel(UserId requester)
    {
        if (!UserId.Equals(requester))
            throw new DomainException("فقط صاحب درخواست می‌تواند آن را لغو کند.");

        EnsurePending("لغو");
        Status = WithdrawalStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        RaiseDomainEvent(new WithdrawalCancelledEvent(Id, UserId));
    }

    private void EnsurePending(string action)
    {
        if (Status != WithdrawalStatus.Pending)
            throw new DomainException($"درخواست در وضعیت '{Status}' قابل {action} نیست.");
    }
}