namespace Application.Wallet.Features.Shared;

public record WalletDto(
    int UserId,
    decimal CurrentBalance,
    decimal ReservedBalance,
    decimal AvailableBalance,
    string Status
);

public record WalletLedgerEntryDto(
    int Id,
    decimal AmountDelta,
    decimal BalanceAfter,
    string TransactionType,
    string ReferenceType,
    int ReferenceId,
    string? Description,
    DateTime CreatedAt
);

public record WalletBalanceResponse(
    decimal Balance,
    decimal Reserved,
    decimal Available
);

/// <summary>
/// DTO for admin wallet adjustments.
/// <see cref="Reason"/> is mandatory and stored in the ledger description for full auditability.
/// </summary>
public record AdminWalletAdjustmentDto(
    decimal Amount,
    string Reason,
    string? Description
);