using Application.Wallet.Contracts;
using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWalletsOverview;

public sealed class GetWalletsOverviewHandler(IWalletQueryService walletQueryService)
    : IQueryHandler<GetWalletsOverviewQuery, PaginatedResult<WalletOverviewDto>>
{
    public async Task<ServiceResult<PaginatedResult<WalletOverviewDto>>> Handle(
        GetWalletsOverviewQuery request,
        CancellationToken ct)
    {
        var filter = new WalletOverviewFilter
        {
            Search = request.Search,
            IsFrozen = request.IsFrozen,
            MinBalance = request.MinBalance,
            MaxBalance = request.MaxBalance,
            CreatedFrom = request.CreatedFrom,
            CreatedTo = request.CreatedTo,
            SortBy = request.SortBy
        };

        var result = await walletQueryService.GetOverviewPageAsync(
            request.Page,
            request.PageSize,
            filter,
            ct);

        return ServiceResult<PaginatedResult<WalletOverviewDto>>.Success(result);
    }
}