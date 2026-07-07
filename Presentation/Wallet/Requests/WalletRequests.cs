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

public sealed record InitiateTopUpRequest(decimal Amount, string Gateway = "zarinpal");

public sealed record RequestWithdrawalRequest(
    decimal Amount,
    string Iban,
    string AccountHolder,
    string? Description = null);

public sealed record RejectWithdrawalRequest(string Reason);

public sealed record MarkWithdrawalPaidRequest(string BankReferenceNumber);

public sealed record GetWithdrawalsListRequest(int Page = 1, int PageSize = 10);

public sealed record GetPendingWithdrawalsListRequest(
    string? Status = null,
    int Page = 1,
    int PageSize = 20);

public sealed record GetWalletLedgerRequest(int Page = 1, int PageSize = 10);

public sealed record FreezeWalletRequest(string Reason);