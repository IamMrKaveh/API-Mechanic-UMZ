using Application.Wallet.Contracts;
using Application.Wallet.Features.Shared;
using Domain.Wallet.Enums;

namespace Application.Wallet.Features.Queries.GetPendingWithdrawals;

public sealed class GetPendingWithdrawalsHandler(
    IWalletWithdrawalQueryService queryService)
    : IQueryHandler<GetPendingWithdrawalsQuery, PaginatedResult<WalletWithdrawalRequestDto>>
{
    public async Task<ServiceResult<PaginatedResult<WalletWithdrawalRequestDto>>> Handle(
        GetPendingWithdrawalsQuery request,
        CancellationToken ct)
    {
        WalletWithdrawalStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<WalletWithdrawalStatus>(request.Status, ignoreCase: true, out var parsed))
        {
            statusFilter = parsed;
        }
        else if (string.IsNullOrWhiteSpace(request.Status))
        {
            statusFilter = WalletWithdrawalStatus.Pending;
        }

        var result = await queryService.GetByStatusAsync(
            statusFilter,
            request.Page,
            request.PageSize,
            request.FromDate,
            request.ToDate,
            ct);

        return ServiceResult<PaginatedResult<WalletWithdrawalRequestDto>>.Success(result);
    }
}