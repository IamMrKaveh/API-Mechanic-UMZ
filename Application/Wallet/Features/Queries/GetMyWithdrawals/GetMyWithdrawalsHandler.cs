using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Wallet.Features.Queries.GetMyWithdrawals;

public sealed class GetMyWithdrawalsHandler(
    IWalletWithdrawalQueryService queryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyWithdrawalsQuery, PaginatedResult<WalletWithdrawalRequestDto>>
{
    public async Task<ServiceResult<PaginatedResult<WalletWithdrawalRequestDto>>> Handle(
        GetMyWithdrawalsQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(currentUserService.UserId.Value);
        var result = await queryService.GetByUserAsync(userId, request.Page, request.PageSize, ct);
        return ServiceResult<PaginatedResult<WalletWithdrawalRequestDto>>.Success(result);
    }
}