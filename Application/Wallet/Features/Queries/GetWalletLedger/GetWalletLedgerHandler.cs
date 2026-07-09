using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Wallet.Features.Queries.GetWalletLedger;

public class GetWalletLedgerHandler(
    IWalletQueryService walletQueryService)
    : IQueryHandler<GetWalletLedgerQuery, PaginatedResult<WalletLedgerEntryDto>>
{
    public async Task<ServiceResult<PaginatedResult<WalletLedgerEntryDto>>> Handle(
        GetWalletLedgerQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        var filter = new WalletLedgerFilter
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            TransactionType = request.TransactionType,
            MinAmount = request.MinAmount,
            MaxAmount = request.MaxAmount,
            SearchTerm = request.SearchTerm
        };

        var result = await walletQueryService.GetLedgerPageAsync(
            userId,
            request.Page,
            request.PageSize,
            filter,
            ct);

        return ServiceResult<PaginatedResult<WalletLedgerEntryDto>>.Success(result);
    }
}