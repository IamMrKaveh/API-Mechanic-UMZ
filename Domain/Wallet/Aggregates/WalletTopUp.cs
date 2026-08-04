using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.Exceptions;
using Domain.Wallet.ValueObjects;
using SharedKernel.Localization;

namespace Domain.Wallet.Aggregates;

public sealed class WalletTopUp : AggregateRoot<WalletTopUpId>
{
    private const decimal MinimumAmount = 10_000m;
    private const string DefaultFailureMarker = "[UNSPECIFIED]";

    public UserId UserId { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;
    public string Gateway { get; private set; } = default!;
    public string? GatewayAuthority { get; private set; }
    public string? GatewayRefId { get; private set; }
    public WalletTopUpStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? FailureReason { get; private set; }

    private WalletTopUp()
    { }

    public static WalletTopUp Initiate(UserId userId, Money amount, string gateway)
    {
        if (userId is null) throw new DomainException("UserId is required.");
        if (amount is null) throw new DomainException("Amount is required.");
        if (string.IsNullOrWhiteSpace(gateway)) throw new DomainException("Gateway is required.");

        if (amount.Amount < MinimumAmount)
            throw new InvalidTopUpAmountException(amount.Amount, MinimumAmount);

        var topUp = new WalletTopUp
        {
            Id = WalletTopUpId.NewId(),
            UserId = userId,
            Amount = amount,
            Gateway = gateway,
            Status = WalletTopUpStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        topUp.RaiseDomainEvent(new WalletTopUpInitiatedEvent(topUp.Id, userId, amount, gateway));
        return topUp;
    }

    public void MarkAuthorityIssued(string authority)
    {
        if (string.IsNullOrWhiteSpace(authority))
            throw new DomainException("Authority cannot be empty.");
        EnsurePending();
        GatewayAuthority = authority;
    }

    public void MarkSucceeded(string refId)
    {
        if (string.IsNullOrWhiteSpace(refId))
            throw new DomainException("Gateway reference id cannot be empty.");
        EnsurePending();

        Status = WalletTopUpStatus.Succeeded;
        GatewayRefId = refId;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletTopUpSucceededEvent(Id, UserId, Amount, refId));
    }

    public void MarkFailed(string reason)
    {
        EnsurePending();

        var effectiveReason = string.IsNullOrWhiteSpace(reason) ? DefaultFailureMarker : reason;

        Status = WalletTopUpStatus.Failed;
        FailureReason = effectiveReason;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletTopUpFailedEvent(Id, UserId, effectiveReason));
    }

    public void MarkCancelled(string reason)
    {
        EnsurePending();
        Status = WalletTopUpStatus.Cancelled;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? DefaultFailureMarker : reason;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WalletTopUpFailedEvent(Id, UserId, FailureReason!));
    }

    private void EnsurePending()
    {
        if (Status != WalletTopUpStatus.Pending)
            throw new DomainException(
                DomainErrorCodes.Wallet.TopUpInvalidState,
                $"TopUp is not modifiable in status '{Status}'.",
                new Dictionary<string, object?> { ["status"] = Status.ToString() });
    }
}
