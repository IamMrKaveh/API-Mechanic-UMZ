namespace Presentation.Wallet.Requests;

public sealed record AdminWalletDebitRequestPayload(
    decimal Amount,
    string Reason,
    string? Description = null,
    int? ExpiryHours = 72,
    string? ReferenceId = null
);

public sealed record RejectWalletDebitRequest(string RejectionReason);
