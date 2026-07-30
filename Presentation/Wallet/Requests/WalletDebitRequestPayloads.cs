namespace Presentation.Wallet.Requests;

public sealed record AdminWalletDebitRequestPayload(
    decimal Amount,
    string Reason,
    string? Description,
    int? ExpiryHours);

public sealed record RejectWalletDebitRequest(string? RejectionReason);
