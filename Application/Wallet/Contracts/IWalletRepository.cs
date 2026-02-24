namespace Application.Wallet.Contracts;

public interface IWalletRepository
{
    /// <summary>Loads wallet snapshot only (no ledger, no reservations).</summary>
    Task<Domain.Wallet.Wallet?> GetByUserIdAsync(
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// Loads wallet snapshot with active reservations and acquires a pessimistic
    /// write lock (SELECT … FOR UPDATE) so concurrent operations are serialized.
    /// </summary>
    Task<Domain.Wallet.Wallet?> GetByUserIdForUpdateAsync(
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a paginated slice of ledger entries directly from the
    /// WalletLedgerEntries table – never loads via the Wallet aggregate.
    /// </summary>
    Task<(List<WalletLedgerEntry> Items, int TotalCount)> GetLedgerPageAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task AddAsync(
        Domain.Wallet.Wallet wallet,
        CancellationToken ct = default);

    void Update(
        Domain.Wallet.Wallet wallet);

    Task<bool> ExistsForUserAsync(
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// Checks the WalletLedgerEntries table directly for an existing idempotency key.
    /// This is a fast, non-aggregate DB query used before executing a credit/debit.
    /// </summary>
    Task<bool> HasIdempotencyKeyAsync(
        int userId,
        string idempotencyKey,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a batch of expired pending reservations for direct processing by the
    /// expiry background service without loading full Wallet aggregates.
    /// </summary>
    Task<List<ExpiredReservationProjection>> GetExpiredReservationBatchAsync(
        int batchSize,
        CancellationToken ct = default);

    /// <summary>
    /// Atomically marks a single reservation as Expired and decrements the wallet's
    /// ReservedBalance – executed with optimistic concurrency on RowVersion.
    /// </summary>
    Task<bool> ExpireReservationAsync(
        int reservationId,
        int walletId,
        decimal amount,
        CancellationToken ct = default);

    /// <summary>
    /// بررسی می‌کند آیا برای یک سفارش مشخص، تراکنش کسر از کیف پول وجود دارد یا خیر.
    /// </summary>
    Task<WalletLedgerEntry?> GetOrderPaymentLedgerEntryAsync(
        int userId,
        int orderId,
        CancellationToken ct = default);
}

/// <summary>Minimal projection used by the expiry background service.</summary>
public record ExpiredReservationProjection(
    int ReservationId,
    int WalletId,
    int UserId,
    decimal Amount,
    int OrderId);