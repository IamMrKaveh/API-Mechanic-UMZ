namespace Domain.Wallet.Interfaces;

public interface IWalletRepository
{
    Task<Aggregates.Wallet?> GetByUserIdForUpdateAsync(int userId, CancellationToken ct = default);

    Task<Aggregates.Wallet?> GetByUserIdAsync(int userId, CancellationToken ct = default);

    Task AddAsync(Aggregates.Wallet wallet, CancellationToken ct = default);

    void Update(Aggregates.Wallet wallet);

    Task<bool> ExistsForUserAsync(int userId, CancellationToken ct = default);

    Task<bool> HasIdempotencyKeyAsync(int userId, string idempotencyKey, CancellationToken ct = default);

    Task<List<ExpiredReservationProjection>> GetExpiredReservationBatchAsync(int batchSize, CancellationToken ct = default);

    Task<bool> ExpireReservationAsync(int reservationId, int walletId, decimal amount, CancellationToken ct = default);
}