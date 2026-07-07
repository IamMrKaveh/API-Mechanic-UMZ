using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetPendingWithdrawals;

public sealed record GetPendingWithdrawalsQuery(
    string? Status = null,
    int Page = 1,
    int PageSize = 20) : IPageQuery<WalletWithdrawalRequestDto>;