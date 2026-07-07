using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetMyWithdrawals;

public sealed record GetMyWithdrawalsQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 10) : IPageQuery<WalletWithdrawalRequestDto>;