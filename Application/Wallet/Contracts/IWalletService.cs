using Application.Wallet.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;

namespace Application.Wallet.Contracts;

public interface IWalletService
{
    Task<ServiceResult<WalletDto>> GetBalanceAsync(
        UserId userId,
        CancellationToken ct = default);

    Task<ServiceResult<PaginatedResult<WalletLedgerEntryDto>>> GetLedgerAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<ServiceResult<Unit>> CreditAsync(
        UserId userId,
        Money amount,
        WalletTransactionType transactionType,
        WalletReferenceType referenceType,
        int referenceId,
        string idempotencyKey,
        string? correlationId = null,
        string? description = null,
        CancellationToken ct = default);

    Task<ServiceResult<Unit>> DebitAsync(
        UserId userId,
        Money amount,
        WalletTransactionType transactionType,
        WalletReferenceType referenceType,
        int referenceId,
        string idempotencyKey,
        string? correlationId = null,
        string? description = null,
        CancellationToken ct = default);

    Task<ServiceResult<Unit>> ReserveAsync(
        UserId userId,
        Money amount,
        OrderId orderId,
        DateTime? expiresAt = null,
        CancellationToken ct = default);

    Task<ServiceResult<Unit>> ReleaseReservationAsync(
        UserId userId,
        OrderId orderId,
        CancellationToken ct = default);
}