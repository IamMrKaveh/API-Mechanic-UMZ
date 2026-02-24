namespace Application.Wallet.Features.Queries.GetWalletLedger;

public record GetWalletLedgerQuery(
    int UserId,
    int Page = 1,
    int PageSize = 20
    ) : IRequest<ServiceResult<PaginatedResult<WalletLedgerEntryDto>>>;