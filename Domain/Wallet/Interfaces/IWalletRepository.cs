using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Interfaces;

public interface IWalletRepository
{
    Task<Aggregates.Wallet?> GetByIdAsync(
        WalletId id,
        CancellationToken ct = default);

    Task<Aggregates.Wallet?> GetByUserIdAsync(
        UserId ownerId,
        CancellationToken ct = default);

    Task<Aggregates.Wallet?> GetByUserIdForUpdateAsync(
        UserId ownerId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Wallet>> GetAllActiveAsync(
        CancellationToken ct = default);

    Task AddAsync(
        Aggregates.Wallet wallet,
        CancellationToken ct = default);

    void Update(
        Aggregates.Wallet wallet);

    Task<bool> ExistsForUserAsync(
        UserId ownerId,
        CancellationToken ct = default);

    Task<bool> HasIdempotencyKeyAsync(
        UserId ownerId,
        string idempotencyKey,
        CancellationToken ct = default);

    Task<IReadOnlyList<WalletLedgerEntry>> GetLedgerEntriesAsync(
        WalletId walletId,
        CancellationToken ct = default);

    Task<WalletLedgerEntry?> GetLedgerEntryByIdAsync(
        WalletLedgerEntryId id,
        CancellationToken ct = default);

    Task<WalletLedgerEntry?> GetLedgerEntryByReferenceAsync(
        WalletId walletId,
        string referenceId,
        CancellationToken ct = default);

    Task AddLedgerEntryAsync(
        WalletLedgerEntry entry,
        CancellationToken ct = default);

    Task<WalletReservation?> GetReservationByIdAsync(
        WalletReservationId reservationId,
        CancellationToken ct = default);
}