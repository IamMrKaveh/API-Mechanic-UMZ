namespace Presentation.Wallet.Requests;

public record AdminWalletAdjustmentRequest(
    decimal Amount,
    string Reason,
    string? Description = null
);

public record CreditWalletRequest(
    decimal Amount,
    string Description,
    string ReferenceId,
    string? IdempotencyKey = null
);

public record DebitWalletRequest(
    decimal Amount,
    string Description,
    string ReferenceId,
    string? IdempotencyKey = null
);

public record ReserveWalletRequest(
    decimal Amount,
    string Purpose
);