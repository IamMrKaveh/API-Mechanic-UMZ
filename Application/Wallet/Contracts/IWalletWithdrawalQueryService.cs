using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;

namespace Application.Wallet.Contracts;

public interface IWalletWithdrawalQueryService
{
    Task<PaginatedResult<WalletWithdrawalRequestDto>> GetByUserAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<PaginatedResult<WalletWithdrawalRequestDto>> GetByStatusAsync(
        WithdrawalStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<WalletWithdrawalRequestDto?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);
}