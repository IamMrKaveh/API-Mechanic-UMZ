using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Wallet.Features.Queries.GetWalletLedger;

public class GetWalletLedgerHandler(IWalletQueryService walletQueryService) : IRequestHandler<GetWalletLedgerQuery, ServiceResult<PaginatedResult<WalletLedgerEntryDto>>>
{
    public async Task<ServiceResult<PaginatedResult<WalletLedgerEntryDto>>> Handle(
        GetWalletLedgerQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        var result = await walletQueryService.GetLedgerPageAsync(
            userId,
            ct);

        var entries = result.Items.Select(e => new WalletLedgerEntryDto(
            e.Id,
            e.WalletId,
            e.UserId,
            e.AmountDelta,
            e.BalanceAfter,
            e.TransactionType.ToString(),
            e.ReferenceType.ToString(),
            e.ReferenceId,
            e.Description,
            e.CreatedAt))
        .ToList();

        var finalResult = PaginatedResult<WalletLedgerEntryDto>.Create(entries, result.TotalCount, request.Page, request.PageSize);

        return ServiceResult<PaginatedResult<WalletLedgerEntryDto>>.Success(finalResult);
    }
}