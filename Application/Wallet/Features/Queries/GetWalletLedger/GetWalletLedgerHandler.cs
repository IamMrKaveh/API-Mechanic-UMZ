using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Wallet.Features.Queries.GetWalletLedger;

public class GetWalletLedgerHandler(
    IWalletQueryService walletQueryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetWalletLedgerQuery, PaginatedResult<WalletLedgerEntryDto>>
{
    public async Task<ServiceResult<PaginatedResult<WalletLedgerEntryDto>>> Handle(
        GetWalletLedgerQuery request,
        CancellationToken ct)
    {
        var resolvedUserId = request.UserId.HasValue && request.UserId.Value != Guid.Empty
            ? request.UserId.Value
            : currentUserService.UserId ?? Guid.Empty;

        if (resolvedUserId == Guid.Empty)
        {
            return ServiceResult<PaginatedResult<WalletLedgerEntryDto>>
                .Unauthorized("کاربر احراز هویت نشده است.");
        }

        var userId = UserId.From(resolvedUserId);

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
