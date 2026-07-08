namespace Application.Wallet.Features.Shared;

public sealed record WalletTransferPreviewDto
{
    public Guid RecipientUserId { get; init; }
    public string RecipientDisplayName { get; init; } = default!;
    public string RecipientPhoneMasked { get; init; } = default!;
    public decimal Amount { get; init; }
    public decimal SenderAvailableBalance { get; init; }
    public decimal DailyLimit { get; init; }
    public decimal AlreadyTransferredToday { get; init; }
    public decimal RemainingDailyLimit { get; init; }
    public bool CanProceed { get; init; }
    public string? Warning { get; init; }
}

public sealed record InitiateWalletTransferResultDto
{
    public Guid TransferId { get; init; }
    public string SenderPhoneMasked { get; init; } = default!;
    public DateTime OtpExpiresAt { get; init; }
    public int OtpTtlSeconds { get; init; }
    public int OtpLength { get; init; }
}

public sealed record ConfirmWalletTransferResultDto
{
    public Guid TransferId { get; init; }
    public string Status { get; init; } = default!;
    public decimal Amount { get; init; }
    public string RecipientDisplayName { get; init; } = default!;
    public string CorrelationId { get; init; } = default!;
    public DateTime CompletedAt { get; init; }
}