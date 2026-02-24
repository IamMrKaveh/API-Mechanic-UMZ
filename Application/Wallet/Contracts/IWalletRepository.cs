namespace Application.Wallet.Contracts;

public interface IWalletRepository
{
    Task<Domain.Wallet.Wallet?> GetByUserIdAsync(
        int userId,
        CancellationToken ct = default
        );

    Task<Domain.Wallet.Wallet?> GetByUserIdWithEntriesAsync(
        int userId,
        CancellationToken ct = default
        );

    Task<Domain.Wallet.Wallet?> GetByUserIdForUpdateAsync(
        int userId,
        CancellationToken ct = default
        );

    Task<(List<WalletLedgerEntry> Items, int TotalCount)> GetLedgerPageAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default
        );

    Task AddAsync(
        Domain.Wallet.Wallet wallet,
        CancellationToken ct = default
        );

    void Update(
        Domain.Wallet.Wallet wallet
        );

    Task<bool> ExistsForUserAsync(
        int userId,
        CancellationToken ct = default
        );

    Task<bool> HasIdempotencyKeyAsync(
        int userId,
        string idempotencyKey,
        CancellationToken ct = default
        );
}