using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWalletsOverview;

public sealed record GetWalletsOverviewQuery(
    string? Search = null,
    bool? IsFrozen = null,
    decimal? MinBalance = null,
    decimal? MaxBalance = null,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null,
    string? SortBy = null,
    int Page = 1,
    int PageSize = 20) : IPageQuery<WalletOverviewDto>;