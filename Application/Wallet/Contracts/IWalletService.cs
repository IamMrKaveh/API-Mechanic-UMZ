namespace Application.Wallet.Contracts;

public interface IWalletService
{
    Task<ServiceResult<WalletDto>> GetBalanceAsync(
        int userId,
        CancellationToken ct = default);

    Task<ServiceResult<PaginatedResult<WalletLedgerEntryDto>>> GetLedgerAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<ServiceResult<Unit>> CreditAsync(
        int userId,
        decimal amount,
        WalletTransactionType transactionType,
        WalletReferenceType referenceType,
        int referenceId,
        string idempotencyKey,
        string? correlationId = null,
        string? description = null,
        CancellationToken ct = default);

    Task<ServiceResult<Unit>> DebitAsync(
        int userId,
        decimal amount,
        WalletTransactionType transactionType,
        WalletReferenceType referenceType,
        int referenceId,
        string idempotencyKey,
        string? correlationId = null,
        string? description = null,
        CancellationToken ct = default);

    Task<ServiceResult<Unit>> ReserveAsync(
        int userId,
        decimal amount,
        int orderId,
        DateTime? expiresAt = null,
        CancellationToken ct = default);

    Task<ServiceResult<Unit>> ReleaseReservationAsync(
        int userId,
        int orderId,
        CancellationToken ct = default);
}