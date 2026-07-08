using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.Exceptions;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Aggregates;

public sealed class WalletTransfer : AggregateRoot<WalletTransferId>
{
    private const decimal MinimumAmount = 10_000m;
    private const int MaxOtpAttempts = 5;

    public UserId FromUserId { get; private set; } = default!;
    public UserId ToUserId { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;
    public string? Description { get; private set; }
    public WalletTransferStatus Status { get; private set; }
    public string OtpHash { get; private set; } = default!;
    public DateTime OtpExpiresAt { get; private set; }
    public int OtpAttempts { get; private set; }
    public string CorrelationId { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? FailureReason { get; private set; }

    private WalletTransfer()
    {
    }

    public static WalletTransfer Initiate(
        UserId fromUserId,
        UserId toUserId,
        Money amount,
        string otpHash,
        TimeSpan otpTtl,
        string? description = null)
    {
        Guard.Against.Null(fromUserId, nameof(fromUserId));
        Guard.Against.Null(toUserId, nameof(toUserId));
        Guard.Against.Null(amount, nameof(amount));
        Guard.Against.NullOrWhiteSpace(otpHash, nameof(otpHash));

        if (fromUserId.Equals(toUserId))
            throw new InvalidWalletTransferException("انتقال به کیف پول خود مجاز نیست.");

        if (amount.Amount < MinimumAmount)
            throw new InvalidWalletTransferException($"حداقل مبلغ انتقال {MinimumAmount:N0} تومان است.");

        if (otpTtl <= TimeSpan.Zero)
            throw new InvalidWalletTransferException("مدت اعتبار کد تأیید نامعتبر است.");

        var id = WalletTransferId.NewId();

        var transfer = new WalletTransfer
        {
            Id = id,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Amount = amount,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Status = WalletTransferStatus.PendingOtp,
            OtpHash = otpHash,
            OtpExpiresAt = DateTime.UtcNow.Add(otpTtl),
            OtpAttempts = 0,
            CorrelationId = id.Value.ToString("N"),
            CreatedAt = DateTime.UtcNow
        };

        transfer.RaiseDomainEvent(new WalletTransferInitiatedEvent(
            transfer.Id, fromUserId, toUserId, amount, transfer.OtpExpiresAt));

        return transfer;
    }

    public void VerifyOtp(string otpHash)
    {
        Guard.Against.NullOrWhiteSpace(otpHash, nameof(otpHash));

        EnsurePendingOtp();

        if (DateTime.UtcNow > OtpExpiresAt)
        {
            Status = WalletTransferStatus.Expired;
            FailureReason = "مهلت وارد کردن کد تأیید به پایان رسیده است.";
            RaiseDomainEvent(new WalletTransferFailedEvent(Id, FromUserId, ToUserId, FailureReason));
            throw new InvalidWalletTransferException(FailureReason);
        }

        if (!string.Equals(OtpHash, otpHash, StringComparison.Ordinal))
        {
            OtpAttempts++;
            var remaining = Math.Max(0, MaxOtpAttempts - OtpAttempts);

            if (OtpAttempts >= MaxOtpAttempts)
            {
                Status = WalletTransferStatus.Failed;
                FailureReason = "تعداد تلاش‌های نامعتبر برای وارد کردن کد تأیید بیش از حد مجاز است.";
                RaiseDomainEvent(new WalletTransferFailedEvent(Id, FromUserId, ToUserId, FailureReason));
            }

            throw new WalletTransferOtpMismatchException(remaining);
        }
    }

    public void MarkCompleted()
    {
        EnsurePendingOtp();

        Status = WalletTransferStatus.Completed;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletTransferCompletedEvent(
            Id, FromUserId, ToUserId, Amount, CorrelationId));
    }

    public void Cancel(UserId requester)
    {
        Guard.Against.Null(requester, nameof(requester));

        if (!FromUserId.Equals(requester))
            throw new InvalidWalletTransferException("فقط ایجادکننده انتقال می‌تواند آن را لغو کند.");

        if (Status != WalletTransferStatus.PendingOtp)
            throw new InvalidWalletTransferException($"انتقال در وضعیت '{Status}' قابل لغو نیست.");

        Status = WalletTransferStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletTransferCancelledEvent(Id, FromUserId, ToUserId));
    }

    public void MarkFailed(string reason)
    {
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        if (Status != WalletTransferStatus.PendingOtp)
            return;

        Status = WalletTransferStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletTransferFailedEvent(Id, FromUserId, ToUserId, reason));
    }

    private void EnsurePendingOtp()
    {
        if (Status != WalletTransferStatus.PendingOtp)
            throw new InvalidWalletTransferException($"انتقال در وضعیت '{Status}' قابل تغییر نیست.");
    }
}