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

public record AdminWalletAdjustmentDto(
    decimal Amount,
    string? Description
);