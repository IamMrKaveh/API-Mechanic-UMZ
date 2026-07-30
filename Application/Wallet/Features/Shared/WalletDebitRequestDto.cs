namespace Application.Wallet.Features.Shared;

public sealed record WalletDebitRequestDto(
    Guid Id,
    Guid WalletId,
    Guid OwnerId,
    decimal Amount,
    string Currency,
    string Reason,
    string? Description,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? RespondedAt,
    string? RejectionReason,
    Guid RequestedBy,
    bool IsExpiringSoon);
