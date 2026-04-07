namespace Presentation.Wallet.Requests;

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