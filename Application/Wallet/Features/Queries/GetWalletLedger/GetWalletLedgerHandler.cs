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

        var result = await walletQueryService.GetLedgerPageAsync(
            userId,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<WalletLedgerEntryDto>>.Success(result);
    }
}